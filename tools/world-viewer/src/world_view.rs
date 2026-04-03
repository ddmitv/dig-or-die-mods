use egui::{Color32, Pos2, Rect, Stroke, TextFormat, Vec2, vec2, pos2};
use log::info;

use crate::{save_model as sm, sprites_info};

pub struct WorldViewer {
    selected_cell: Option<(usize, usize)>,
    view_offset: Vec2,
    view_zoom: f32,

    map_texture: Option<egui::TextureHandle>,
    layers_vis: LayersVisibility,
    needs_texture_update: bool,

    item_spritesheet: egui::TextureHandle,
    unit_spritesheet: egui::TextureHandle,

    go_to_window_open: bool,
}

#[derive(Debug)]
struct LayersVisibility {
    pub background: bool,
    pub backwall: bool,
    pub main: bool,
    pub water: bool,
    pub light_mode: LightMode,

    pub units: bool,
    pub monster_units: bool,
    pub ally_units: bool,
    pub pickups: bool,
    pub players: bool,

    pub units_hover: bool,
    pub pickups_hover: bool,
    pub players_hover: bool,
}

#[derive(PartialEq, Debug)]
enum LightMode { FullBrightness, Rgb, Monochrome }

impl Default for LayersVisibility {
    fn default() -> Self {
        return Self {
            background: true, backwall: true, main: true, water: true,
            light_mode: LightMode::FullBrightness,
            units: true, monster_units: true, ally_units: true,
            pickups: true, players: true,
            units_hover: false, pickups_hover: false, players_hover: false
        };
    }
}

impl WorldViewer {
    pub fn new(ctx: &egui::Context) -> Self {
        return Self {
            selected_cell: None,
            view_offset: Vec2::ZERO,
            view_zoom: 1.0,

            map_texture: None,
            layers_vis: LayersVisibility::default(),
            needs_texture_update: true,

            item_spritesheet: Self::load_item_spritesheet_texture(ctx),
            unit_spritesheet: Self::load_unit_spritesheet_texture(ctx),

            go_to_window_open: false,
        };
    }
    pub fn reset(&mut self) {
        self.selected_cell = None;
        self.view_offset = Vec2::ZERO;
        self.view_zoom = 1.0;
        self.map_texture = None;
        self.layers_vis = LayersVisibility::default();
        self.needs_texture_update = true;
        self.go_to_window_open = false;
    }

    pub fn show(&mut self, ui: &mut egui::Ui, save: &mut sm::SaveModel) {
        if self.needs_texture_update {
            self.update_layers_texture(ui, save);
            self.needs_texture_update = false;
        }

        egui::Panel::top("world_layers")
            .frame(egui::Frame::new().inner_margin(4))
            .show_inside(ui, |ui| {
                ui.horizontal_wrapped(|ui| {
                    self.show_world_layer_contents(ui, save);
                })
            });

        if self.selected_cell.is_some() {
            egui::Panel::right("cell_inspector_panel")
                .min_size(200.0)
                .show_inside(ui, |ui| {
                    self.show_cell_inspector_contents(ui, save);
                });
        } else {
            ui.skip_ahead_auto_ids(1);
        }

        egui::CentralPanel::default().show_inside(ui, |ui| {
            self.show_cell_grid(ui, save);
        });
    }

    fn load_item_spritesheet_texture(ctx: &egui::Context) -> egui::TextureHandle {
        info!("Loading item spritesheet...");
        let bytes = include_bytes!("../assets/item-spritesheet.png");
        let image = image::load_from_memory_with_format(bytes, image::ImageFormat::Png).expect("Failed to decode item spritesheet image");
        info!("Item spritesheet image dimension: {}x{}, format: {:?}, bytes: {}", image.width(), image.height(), image.color(), image.as_bytes().len());

        let image_size = [image.width() as usize, image.height() as usize];
        let color_image = egui::ColorImage::from_rgba_premultiplied(image_size, image.into_rgba8().as_raw());

        return ctx.load_texture("item_spritesheet", color_image, egui::TextureOptions::NEAREST);
    }
    fn load_unit_spritesheet_texture(ctx: &egui::Context) -> egui::TextureHandle {
        info!("Loading unit spritesheet...");
        let bytes = include_bytes!("../assets/unit-spritesheet.png");
        let image = image::load_from_memory_with_format(bytes, image::ImageFormat::Png).expect("Failed to decode unit spritesheet image");
        info!("Unit spritesheet image dimension: {}x{}, format: {:?}, bytes: {}", image.width(), image.height(), image.color(), image.as_bytes().len());

        let image_size = [image.width() as usize, image.height() as usize];
        let color_image = egui::ColorImage::from_rgba_premultiplied(image_size, image.into_rgba8().as_raw());

        return ctx.load_texture("unit_spritesheet", color_image, egui::TextureOptions::NEAREST);
    }

    fn update_layers_texture(&mut self, ui: &mut egui::Ui, save: &mut sm::SaveModel) {
        info!("Updating map texture with layer visibility: {:?}", self.layers_vis);

        let (world_width, world_height) = save.cell_grid.dimensions();
        let mut pixels = vec![Color32::default(); world_width * world_height];
        for y in 0..world_height {
            for x in 0..world_width {
                let src_idx = y * world_width + x;
                let dst_idx = (world_width - 1 - x) * world_height + y;
                pixels[dst_idx] = cell_to_color(save.cell_grid.flatten_at(src_idx), &self.layers_vis);
            }
        }
        let color_image = egui::ColorImage::new([world_height, world_width], pixels);
        self.map_texture = Some(ui.load_texture("map_texture", color_image, egui::TextureOptions::NEAREST));

        fn cell_to_color(cell: &sm::Cell, layers_info: &LayersVisibility) -> Color32 {
            let mut color = render_full_brightness(cell, layers_info);
            match layers_info.light_mode {
                LightMode::Rgb => color = color * cell.light.to_color32(),
                LightMode::Monochrome => { color = color.gamma_multiply(cell.light.to_color32().intensity()); color[3] = 255; },
                LightMode::FullBrightness => (),
            }
            return color;
        }
        fn render_full_brightness(cell: &sm::Cell, vis: &LayersVisibility) -> Color32 {
            let mut color = Color32::BLACK;
            if vis.main && cell.content_id != 0 {
                return cell.get_cell_color();
            } else if vis.background && let Some(bg_surface) = cell.bg_surface() {
                color = if vis.backwall && cell.has_backwall() {
                    Color32::from_rgb(126, 124, 117)
                } else {
                    bg_surface.color
                }
            }
            if vis.water && cell.water != 0.0 {
                let liquid_color = if cell.is_lava() { Color32::from_rgb(190, 45, 24) } else { Color32::from_rgb(44, 85, 113) };
                if cell.water <= 0.3 {
                    let blend_factor = ease_out_quad(cell.water / 0.3);

                    color = color.lerp_to_gamma(liquid_color, blend_factor);
                } else if !cell.is_lava() {
                    let depth = cell.water - 0.3;
                    let darkness_factor = (1.0 - depth / 60.0).max(0.5);

                    color = liquid_color.gamma_multiply(darkness_factor);
                } else {
                    let depth = cell.water - 0.3;
                    let brightness_factor = 1.0 + depth / 60.0;

                    color = liquid_color.gamma_multiply(brightness_factor);
                }
            }
            return color;
        }
    }

    fn show_cell_grid(&mut self, ui: &mut egui::Ui, save: &mut sm::SaveModel) {
        let (world_width, world_height) = save.cell_grid.dimensions();
        let resp = ui.allocate_rect(ui.available_rect_before_wrap(), egui::Sense::click_and_drag());

        if resp.dragged() {
            self.view_offset -= resp.drag_delta() / self.view_zoom;
        }
        if resp.hovered() {
            let scroll_delta = ui.input(|i| i.smooth_scroll_delta.x + i.smooth_scroll_delta.y);

            if scroll_delta != 0.0 {
                let scroll_zoom_speed = ui.options(|opt| opt.input_options.scroll_zoom_speed);
                let old_scale = self.view_zoom;
                self.view_zoom *= 1.0 + scroll_delta * scroll_zoom_speed;
                if let Some(mouse_pos) = ui.pointer_interact_pos() {
                    let viewport_pos = mouse_pos - resp.rect.min;
                    self.view_offset += viewport_pos / old_scale - viewport_pos / self.view_zoom;
                }
            }
        }
        let image_rect = Rect::from_min_size(
            self.world_to_screen(Vec2::ZERO, &resp.rect),
            vec2(world_height as f32, world_width as f32) * self.view_zoom
        );
        let painter = ui.painter_at(resp.rect);
        if let Some(map_texture) = &self.map_texture {
            let image_uv = Rect::from_min_max(pos2(0.0, 0.0), pos2(1.0, 1.0));
            painter.image(map_texture.id(), image_rect, image_uv, Color32::WHITE);
        }
        let mut tooltip_shown = false;
        let block_tooltips = ui.input(|i| i.modifiers.shift);
        let tooltip_widget_id = ui.make_persistent_id("world_viewer_tooltip"); // same ID allows for tooltip stacking

        if self.layers_vis.units {
            for (unit_idx, unit) in save.units.iter().enumerate() {
                let show_unit = match unit.monster_data {
                    Some(_) => self.layers_vis.monster_units,
                    None => self.layers_vis.ally_units,
                };
                if !show_unit { continue; }

                let center = self.world_to_screen(self.grid_to_world_pos(unit.pos, &save.cell_grid), &resp.rect);
                
                let scale = if self.layers_vis.units_hover { 8.0 } else { 0.7 * self.view_zoom };

                let interact_rect: Rect = if let Some(sprite_info) = unit.desc_id.and_then(|id| sprites_info::UNIT_SPRITES_INFO.get(id.get() as usize)) 
                                                && !sprite_info.u0.is_nan() {
                    let icon_uv = Rect::from_min_max(pos2(sprite_info.u0, sprite_info.v0), pos2(sprite_info.u1, sprite_info.v1));
                    let icon_size = 0.02 * scale * Vec2::new(sprite_info.width as f32, sprite_info.height as f32);
                    let icon_rect = Rect::from_center_size(center, icon_size);

                    painter.image(self.unit_spritesheet.id(), icon_rect, icon_uv, Color32::WHITE);
                    // painter.rect_filled(icon_rect, 0.0, Color32::RED);

                    icon_rect
                } else {
                    painter.circle_filled(center, scale, Color32::PURPLE);

                    Rect::from_center_size(center, Vec2::splat(scale * 2.0))
                };

                if !block_tooltips && !self.layers_vis.units_hover && let Some(mouse) = ui.pointer_interact_pos() && interact_rect.contains(mouse) {
                    tooltip_shown = true;
                    egui::Tooltip::always_open(ui.ctx().clone(), ui.layer_id(), tooltip_widget_id, center).show(|ui| {
                        ui.style_mut().wrap_mode = Some(egui::TextWrapMode::Extend);

                        ui.heading(format!("Unit[{unit_idx}]"));
                        ui.label(format!("Position: {:.6}", unit.pos));
                        ui.label(format!("Codename: {:?}", unit.code_name));
                        ui.label(format!("Instance ID: {}", unit.instance_id));
                        ui.label(format!("Descriptor ID: {}", unit.desc_id.map_or("?".to_owned(), |x| x.to_string())));
                        ui.label(format!("HP: {}", unit.hp));
                        ui.label(format!("Air: {}", unit.air));
                        if let Some(sm::UnitMonsterData { is_night_spawn, target_id, is_creative_spawn }) = unit.monster_data {
                            ui.group(|ui| {
                                ui.weak("Monster specific:");
                                ui.checkbox(&mut is_night_spawn.clone(), "Night Spawn");
                                ui.label(format!("Target ID: {}", if target_id == u16::MAX { "[None]".to_owned() } else { target_id.to_string() } ));
                                ui.checkbox(&mut is_creative_spawn.clone(), "Creative Spawn");
                            });
                        }
                    });
                }
            }
        }
        if self.layers_vis.pickups {
            for (pickup_idx, pickup) in save.pickups.iter().enumerate() {
                let center = self.world_to_screen(self.grid_to_world_pos(pickup.pos + sm::Vector2::DOWN, &save.cell_grid), &resp.rect);
                let icon_size = if self.layers_vis.pickups_hover { 30.0 } else { 1.3 * self.view_zoom };
                let icon_rect = Rect::from_center_size(center, Vec2::splat(icon_size));

                if let Some(sprite_info) = sprites_info::ITEM_SPRITES_INFO.get(pickup.id as usize) && !sprite_info.u0.is_nan() {
                    let icon_uv = Rect::from_min_max(pos2(sprite_info.u0, sprite_info.v0), pos2(sprite_info.u1, sprite_info.v1));
                    painter.image(self.item_spritesheet.id(), icon_rect, icon_uv, Color32::WHITE);
                } else {
                    painter.rect_filled(icon_rect, 0, Color32::PURPLE);
                }

                if !block_tooltips && !self.layers_vis.pickups_hover && let Some(mouse) = ui.pointer_interact_pos() && icon_rect.contains(mouse) {
                    tooltip_shown = true;
                    egui::Tooltip::always_open(ui.ctx().clone(), ui.layer_id(), tooltip_widget_id, center).show(|ui| {
                        ui.style_mut().wrap_mode = Some(egui::TextWrapMode::Extend);

                        ui.heading(format!("Pickup[{pickup_idx}]"));
                        ui.label(format!("Position: {:.6}", pickup.pos));
                        ui.label(item_name_widget_text(pickup.id));
                        ui.label(format!("Item ID: {}", pickup.id));
                        ui.label(format!("Creation time: {:} ({:.5} ago)", pickup.creation_time, save.vars.m_simuTimeD - (pickup.creation_time as f64)));
                    });
                }
            }
        }
        if self.layers_vis.players {
            for (player_idx, player) in save.players.iter().enumerate() {
                let center = self.world_to_screen(self.grid_to_world_pos(player.pos, &save.cell_grid), &resp.rect);

                let radius = if self.layers_vis.players_hover { 10.0 } else { 0.7 * self.view_zoom };

                painter.circle_filled(center, radius, Color32::ORANGE);
                let interact_rect = Rect::from_center_size(center, Vec2::splat(radius * 2.0));
                if !block_tooltips && !self.layers_vis.players_hover && let Some(mouse) = ui.pointer_interact_pos() && interact_rect.contains(mouse) {
                    tooltip_shown = true;
                    egui::Tooltip::always_open(ui.ctx().clone(), ui.layer_id(), tooltip_widget_id, center).show(|ui| {
                        ui.style_mut().wrap_mode = Some(egui::TextWrapMode::Extend);

                        ui.heading(format!("Player[{player_idx}]"));
                        ui.label(format!("Steam ID: {}", player.steam_id));
                        ui.label(format!("Name: {:?}", player.name));
                        ui.label(format!("Position: {}", player.pos));
                        ui.label(format!("Unit ID: {}", player.unit_player_id));
                        ui.group(|ui| {
                            ui.weak("Appearance:");
                            ui.label(format!("Female: {}", player.skin_is_female));
                            ui.horizontal(|ui| {
                                ui.label("Color skin:");
                                show_color_indicator(ui, vec2(10.0, 10.0), Color32::from_gray((player.skin_color_skin * 255.0) as u8));
                                ui.label(player.skin_color_skin.to_string());
                            });
                            ui.label(format!("Hair style: {}", player.skin_hair_style));
                            ui.horizontal(|ui| {
                                ui.label("Hair color:");
                                show_color_indicator(ui, vec2(10.0, 10.0), player.skin_color_hair.to_color32());
                                ui.label(player.skin_color_hair.to_string());
                            });
                            ui.horizontal(|ui| {
                                ui.label("Eyes color:");
                                show_color_indicator(ui, vec2(10.0, 10.0), player.skin_color_eyes.to_color32());
                                ui.label(player.skin_color_eyes.to_string());
                            });
                        });
                    });
                }
            }
        }
        
        if let Some(mouse) = resp.hover_pos() {
            let world_pos = self.screen_to_world(mouse, &resp.rect);
            let world_bounds = Rect::from_min_size(Pos2::ZERO, cell_pos_to_vec2((world_height, world_width)));
            if world_bounds.contains(world_pos.to_pos2()) {
                if !block_tooltips && !tooltip_shown {
                    let (cell_x, cell_y) = self.world_grid_flip_y((world_pos.x as usize, world_pos.y as usize), &save.cell_grid);
                    resp.clone().on_hover_text_at_pointer(format!("({cell_x}, {cell_y})"));
                }

                let highlight_rect = Rect::from_min_size(self.world_to_screen(world_pos.floor(), &resp.rect), Vec2::splat(self.view_zoom));
                let stroke = Stroke::new(2.0, const { Color32::from_rgba_unmultiplied_const(255, 255, 255, 100) });
                painter.rect_stroke(highlight_rect, 1.0, stroke, egui::StrokeKind::Middle);

                if resp.clicked() {
                    self.selected_cell = Some((world_pos.x as usize, world_pos.y as usize));
                    info!("Selected cell at {}", world_pos);
                }
            }
        }
        if let Some((mut cell_x, mut cell_y)) = self.selected_cell {
            if ui.input(|i| i.key_pressed(egui::Key::ArrowLeft)) && cell_x > 0 {
                cell_x -= 1;
            }
            if ui.input(|i| i.key_pressed(egui::Key::ArrowRight)) && cell_x + 1 < world_width {
                cell_x += 1;
            }
            if ui.input(|i| i.key_pressed(egui::Key::ArrowDown)) && cell_y + 1 < world_height {
                cell_y += 1;
            }
            if ui.input(|i| i.key_pressed(egui::Key::ArrowUp)) && cell_y > 0 {
                cell_y -= 1;
            }
            self.selected_cell = Some((cell_x, cell_y));
            
            let highlight_rect = Rect::from_min_size(self.world_to_screen(cell_pos_to_vec2((cell_x, cell_y)), &resp.rect), Vec2::splat(self.view_zoom));
            let stroke = Stroke::new(3.0, const { Color32::BLUE });
            painter.rect_stroke(highlight_rect, 1.0, stroke, egui::StrokeKind::Middle);
        }
        if ui.input_mut(|i| i.consume_key(egui::Modifiers::NONE, egui::Key::Escape)) {
            self.selected_cell = None;
            self.go_to_window_open = false;
            info!("Closed all windows/panels via Escape key");
        }

        let mut is_go_to_window_opened = false;
        if ui.input_mut(|i| i.consume_key(egui::Modifiers::CTRL, egui::Key::G)) {
            self.go_to_window_open = true;
            is_go_to_window_opened = true;
            info!("Opened the go to window");
        }
        if self.go_to_window_open {
            let builder = egui::UiBuilder::new()
                .max_rect(resp.rect)
                .layout(egui::Layout::right_to_left(egui::Align::Min))
                .id_salt("go_to_window");
            ui.scope_builder(builder, |ui| {
                egui::Frame::window(ui.style()).show(ui, |ui| {
                    ui.with_layout(egui::Layout::right_to_left(egui::Align::TOP), |ui| {
                        if ui.button("❌").clicked() {
                            self.go_to_window_open = false;
                        }
                    });
                    let egui::InnerResponse { inner: pos, response } = grid_pos_field(ui, "go_to_field".into(), save.cell_grid.dimensions());
                    if let Some(pos) = pos && save.cell_grid.is_valid_pos(pos) {
                        let viewer_pos = self.world_grid_flip_y(pos, &save.cell_grid);
                        self.selected_cell = Some(viewer_pos);
                        self.go_to_window_open = false;

                        self.zoom_to_position(cell_pos_to_vec2(viewer_pos), 10.0, &resp.rect);
                    }
                    if is_go_to_window_opened {
                        response.request_focus();
                    }
                });
            });
        }

        if ui.input_mut(|i| i.consume_key(egui::Modifiers::CTRL, egui::Key::Space))
            && let Some(cell_pos) = self.selected_cell {
            self.zoom_to_position(cell_pos_to_vec2(cell_pos), 10.0, &resp.rect);
        }

        if ui.input_mut(|i| i.consume_key(egui::Modifiers::NONE, egui::Key::F)) {
            let (world_width, world_height) = save.cell_grid.dimensions();
            
            let world_size = vec2(world_height as f32, world_width as f32);
            let viewport_size = resp.rect.size();

            let zoom_x = viewport_size.x / world_size.x;
            let zoom_y = viewport_size.y / world_size.y;
            let zoom = zoom_x.min(zoom_y);

            let center = world_size * 0.5;

            self.zoom_to_position(center, zoom, &resp.rect);
            info!("Zoomed to fit world (Zoom: {})", zoom);
        }
    }

    fn show_cell_inspector_contents(&mut self, ui: &mut egui::Ui, save: &mut sm::SaveModel) {
        let (world_cell_x, world_cell_y) = self.selected_cell.expect("selected_cell should be some");
        let (cell_x, cell_y) = self.world_grid_flip_y((world_cell_x, world_cell_y), &save.cell_grid);
        let cell = save.cell_grid.get(cell_x, cell_y).expect("invalid selected cell position");

        ui.add_space(4.0);
        ui.vertical_centered(|ui| {
            ui.heading("Cell Inspector");
        });
        ui.separator();

        ui.label(format!("Position: ({cell_x}, {cell_y})"));
        ui.label(item_name_widget_text(cell.content_id));
        ui.label(format!("Content ID: {}", cell.content_id));
        ui.label(format!("HP: {}", cell.content_hp));
        ui.horizontal(|ui| {
            ui.label("Water:");
            let (rect, _) = ui.allocate_exact_size(vec2(10.0, 10.0), egui::Sense::hover());
            
            let filled_rect = Rect::from_min_max(rect.left_bottom() - vec2(0.0, rect.height() * cell.water.clamp(0.0, 1.0)), rect.max);
            let liquid_color = if cell.is_lava() { Color32::RED } else { Color32::BLUE };
            ui.painter().rect_filled(filled_rect, 1.0, liquid_color);
            ui.painter().rect_stroke(rect, 1.0, Stroke::new(1.0, ui.visuals().text_color()), egui::StrokeKind::Outside);

            ui.label(format!("{:.15}", &cell.water));
        });
        ui.horizontal(|ui| {
            ui.label("Force:");
            let (rect, _) = ui.allocate_exact_size(vec2(10.0, 10.0), egui::Sense::hover());
            
            ui.painter().circle_stroke(rect.center(), 10.0, Stroke::new(1.0, ui.visuals().text_color()));
            ui.painter().arrow(rect.center(), vec2(cell.force_x as f32, -cell.force_y as f32).normalized() * 8.0, Stroke::new(1.7, ui.visuals().text_color()));

            ui.label(format!("({:+}, {:+})", cell.force_x, cell.force_y));
        });
        ui.horizontal(|ui| {
            ui.label("Light:");
            show_color_indicator(ui, vec2(10.0, 10.0), cell.light.to_color32());

            ui.label(format!("({}, {}, {})", cell.light.r, cell.light.g, cell.light.b));
        });
        ui.label({
            let mut job = egui::text::LayoutJob::default();
            job.append("Background: ", 0.0, TextFormat::default());
            if let Some(background) =  cell.bg_surface() {
                job.append(background.name, 0.0, TextFormat::simple(egui::FontId::default(), background.color.gamma_multiply(1.65)));
            } else {
                job.append("None", 0.0, TextFormat::default());
            }
            job
        });

        ui.collapsing("Flags", |ui| {
            ui.disable();
            ui.checkbox(&mut (cell.flags & sm::Cell::FLAG_IS_LAVA != 0), "🌋 Lava");
            ui.checkbox(&mut (cell.flags & sm::Cell::FLAG_IS_BURNING != 0), "🔥 Burning");
            ui.checkbox(&mut (cell.flags & sm::Cell::FLAG_IS_MAPPED != 0), "🗺 Mapped");
            ui.checkbox(&mut (cell.flags & sm::Cell::FLAG_IS_X_REVERSED != 0), "🔄 Reversed");
            ui.checkbox(&mut (cell.flags & sm::Cell::FLAG_WATER_FALL != 0), "🌊 Waterfall");
        });
    }

    fn show_world_layer_contents(&mut self, ui: &mut egui::Ui, save: &mut sm::SaveModel) {
        let vis = &mut self.layers_vis;
        let mut layer_changed = false;
        layer_changed |= {
            let resp = ui.toggle_value(&mut vis.background, egui::Atoms::new("Background"));
            resp.context_menu(|ui| {
                layer_changed |= ui.add_enabled(vis.background, egui::Checkbox::new(&mut vis.backwall, "Show Backwall")).changed();
            });
            resp.changed()
        };
        layer_changed |= ui.toggle_value(&mut vis.main, "Main").changed();
        layer_changed |= ui.toggle_value(&mut vis.water, "Water").changed();
        ui.separator();
        layer_changed |= resettable_toggle_value(ui, &mut vis.light_mode, LightMode::Rgb, LightMode::FullBrightness, "RGB Light").changed();
        layer_changed |= resettable_toggle_value(ui, &mut vis.light_mode, LightMode::Monochrome, LightMode::FullBrightness, "Monochrome Light").changed();
        ui.separator();
        {
            let resp = ui.toggle_value(&mut vis.units, "Units");
            vis.units_hover = resp.hovered();
            resp.context_menu(|ui| {
                ui.add_enabled_ui(vis.units, |ui| {
                    ui.checkbox(&mut vis.monster_units, "Monsters");
                    ui.checkbox(&mut vis.ally_units, "Allies");
                });
            });
            resp.on_hover_ui(|ui| {
                ui.label(format!("{} units", save.units.len()));
            });
        }
        {
            let resp = ui.toggle_value(&mut vis.pickups, "Pickups");
            vis.pickups_hover = resp.hovered();
            resp.on_hover_ui(|ui| {
                ui.label(format!("{} pickups", save.pickups.len()));
            });
        }
        {
            let resp = ui.toggle_value(&mut vis.players, "Players");
            vis.players_hover = resp.hovered();
            resp.on_hover_ui(|ui| {
                ui.label(format!("{} players", save.players.len()));
            });
        }
        if layer_changed {
            info!("Some layer has been changed, need to update map texture");
            self.needs_texture_update = true;
        }
    }

    fn screen_to_world(&self, screen_pos: Pos2, viewport_rect: &Rect) -> Vec2 {
        return ((screen_pos - viewport_rect.min) / self.view_zoom) + self.view_offset;
    }
    fn world_to_screen(&self, world_pos: Vec2, viewport_rect: &Rect) -> Pos2 {
        return viewport_rect.min + (world_pos - self.view_offset) * self.view_zoom;
    }
    fn world_grid_flip_y(&self, (world_x, world_y): (usize, usize), grid: &sm::CellGrid) -> (usize, usize) {
        return (world_x, grid.width() - 1 - world_y);
    }
    fn grid_to_world_pos(&self, grid_pos: sm::Vector2, grid: &sm::CellGrid) -> Vec2 {
        return vec2(grid_pos.x, grid.width() as f32 - 1.0 - grid_pos.y);
    }

    fn zoom_to_position(&mut self, world_pos: Vec2, zoom: f32, viewport_rect: &Rect) {
        self.view_zoom = zoom;
        let half_viewport = viewport_rect.size() / 2.0;
        self.view_offset = world_pos - half_viewport / self.view_zoom;
    }
}

fn linear_multiply_by_f32((r, g, b): (u8, u8, u8), factor: f32) -> (u8, u8, u8) {
    return ((r as f32 * factor) as u8, (g as f32 * factor) as u8, (b as f32 * factor) as u8);
}
fn color_min_elementwise((r, g, b): (u8, u8, u8), (m_r, m_g, m_b): (u8, u8, u8)) -> (u8, u8, u8) {
    return (r.min(m_r), g.min(m_g), b.min(m_b));
}
fn ease_out_quad(t: f32) -> f32 {
    return t * (2.0 - t);
}
fn cell_pos_to_vec2((x, y): (usize, usize)) -> egui::Vec2 {
    return vec2(x as f32, y as f32);
}

fn resettable_toggle_value<T: PartialEq>(ui: &mut egui::Ui, current_value: &mut T, selected_value: T, reset_to: T, label: &str) -> egui::Response {
    let mut resp = ui.selectable_label(*current_value == selected_value, label);
    if resp.clicked() {
        if *current_value == selected_value {
            *current_value = reset_to;
        } else {
            *current_value = selected_value;
        }
        resp.mark_changed();
    }
    return resp;
}

fn grid_pos_field(ui: &mut egui::Ui, id: egui::Id, (width, height): (usize, usize)) -> egui::InnerResponse<Option<(usize, usize)>> {
    use egui::{InnerResponse, Key};
    let mut temp_text = ui.data_mut(|data| data.remove_temp::<String>(id)).unwrap_or_default();
    
    let response = ui.add(egui::TextEdit::singleline(&mut temp_text)
        .id(id)
        .desired_width(150.0)
        .clip_text(false)
        .prefix("Go To:")
        .suffix("🔎")
        .hint_text("x y")
    ).on_hover_text(
        "Enter cell coordinates (e.g., \"10, 5\", \"60 200\").\n\
        • Negative values count from end (-1 = last, e.g., \"-100 300\" ➡ (924, 300)).\n\
        • Single value sets both coordinates (e.g., \"7\" ➡ (7, 7)).");

    if response.lost_focus() && ui.input(|i| i.key_pressed(Key::Enter)) {
        return InnerResponse::new(parse_grid_pos(&temp_text, (width, height)), response);
    } else {
        ui.data_mut(|data| data.insert_temp(id, temp_text));
    }
    return InnerResponse::new(None, response);

    fn norm_coord(value: i16, max: usize) -> Option<usize> {
        let base = if value < 0 { max } else { 0 };
        return base.checked_add_signed(value as isize).filter(|&x| x < max);
    }
    fn parse_grid_pos(text: &str, (width, height): (usize, usize)) -> Option<(usize, usize)> {
        let parts: Vec<&str> = text
            .trim_matches(['(', ')', '[', ']'])
            .split([',', ';', ':', ' '])
            .map(|p| p.trim_ascii())
            .filter(|c| !c.is_empty())
            .collect();

        if parts.len() == 1 {
            let xy = parts[0].parse::<i16>().ok()?;
            return Some((norm_coord(xy, width)?, norm_coord(xy, height)?));
        }
        if parts.len() != 2 { return None; }
        let x = parts[0].parse::<i16>().ok()?;
        let y = parts[1].parse::<i16>().ok()?;
        return Some((norm_coord(x, width)?, norm_coord(y, height)?));
    }
}

fn item_name_widget_text(content_id: u16) -> egui::WidgetText {
    let item_color = sm::get_content_color(content_id);
    let (item_codename, item_name) = sm::get_content_name(content_id);

    let mut job = egui::text::LayoutJob::default();
    job.append("Name: ", 0.0, TextFormat::default());
    job.append(item_name, 0.0, TextFormat::simple(egui::FontId::default(), item_color));
    job.append(item_codename, 6.0, TextFormat::simple(egui::FontId::monospace(13.0), Color32::GRAY));
    return job.into();
}

fn show_color_indicator(ui: &mut egui::Ui, size: Vec2, color: Color32) {
    let (rect, _) = ui.allocate_exact_size(size, egui::Sense::hover());
            
    ui.painter().rect_filled(rect, 1.0, color);
    ui.painter().rect_stroke(rect, 1.0, Stroke::new(1.0, ui.visuals().widgets.noninteractive.bg_stroke.color), egui::StrokeKind::Outside);
}