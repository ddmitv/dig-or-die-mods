use std::sync::mpsc;

use anyhow::Context;
use eframe::egui;
use egui::{Color32, RichText};
use log::{error, info};

use crate::{misc::lzf2, save_model, world_view::WorldViewer};

struct ChannelPayload {
    pub save: anyhow::Result<save_model::SaveModel>,
    pub file_name: String,
}

pub struct App {
    save: Option<save_model::SaveModel>,
    msg_chs: (mpsc::Sender<ChannelPayload>, mpsc::Receiver<ChannelPayload>),
    save_loading_err: Option<anyhow::Error>,
    save_abnormalities: Option<save_model::SaveAbnormalities>,
    world_viewer: WorldViewer,
}

impl App {
    pub fn new(cc: &eframe::CreationContext<'_>) -> Self {
        cc.egui_ctx.set_visuals(egui::Visuals {
            widgets: egui::style::Widgets {
                noninteractive: egui::style::WidgetVisuals {
                    corner_radius: egui::CornerRadius::same(5),
                    ..egui::Visuals::dark().widgets.noninteractive
                },
                ..Default::default()
            },
            ..Default::default()
        });
        return Self {
            save: None, msg_chs: mpsc::channel(),
            save_loading_err: None,
            save_abnormalities: None,
            world_viewer: WorldViewer::new(&cc.egui_ctx),
        };
    }

    fn ui_file_drag_and_drop(&mut self, ctx: &egui::Context) {
        use egui::{Align2, Color32, Id, LayerId, Order, FontId};
        use eframe::emath::GuiRounding as _;

        let hovered_files_len = ctx.input(|i| i.raw.hovered_files.len());
        if hovered_files_len > 0 {
            let painter = ctx.layer_painter(LayerId::new(Order::Foreground, Id::new("file_drop_target")));

            let content_rect = ctx.content_rect();
            painter.rect_filled(content_rect, 0.0, Color32::from_black_alpha(192));
            let (text, color) = if hovered_files_len == 1 {
                ("⬇ Drop save file here ⬇", Color32::WHITE)
            } else {
                ("❌ Please only drop single save file here ❌", Color32::YELLOW)
            };
            painter.text(content_rect.center().round_to_pixels(painter.pixels_per_point()), Align2::CENTER_CENTER, text, FontId::proportional(32.0), color);
        }
        if let Some(dropped_file) = ctx.input(|i| match &i.raw.dropped_files[..] { [single] => Some(single.clone()), _ => None }) {
            let tx = self.msg_chs.0.clone();

            #[cfg(target_arch = "wasm32")]
            if let Some(bytes) = dropped_file.bytes {
                let task = async move {
                    let save = Self::process_save_bytes(&bytes);
                    let _ = tx.send(ChannelPayload { save, file_name: dropped_file.name });
                };
                wasm_bindgen_futures::spawn_local(task);
                return;
            }
            #[cfg(not(target_arch = "wasm32"))]
            if let Some(path) = dropped_file.path {
                std::thread::spawn(move || {
                    let save = std::fs::read(&path).context("Failed to read file")
                                .and_then(|bytes| Self::process_save_bytes(&bytes));

                    _ = tx.send(ChannelPayload { save, file_name: dropped_file.name });
                });
                return;
            }
            error!("Failed to extract bytes from file");
        }
    }

    fn process_save_bytes(raw_bytes: &[u8]) -> anyhow::Result<save_model::SaveModel> {
        const UNCOMPRESSED_SAVE_MAGIC: [u8; 10] = [ 0x09, 0x53, 0x41, 0x56, 0x45, 0x20, 0x46, 0x49, 0x4C, 0x45 ]; // "\x09SAVE FILE"

        let decompressed_bytes = if raw_bytes.starts_with(&UNCOMPRESSED_SAVE_MAGIC) {
            info!("Detected uncompressed save format");
            raw_bytes
        } else {
            info!("Detected compressed LZF2 save format");
            &lzf2::lzf2_decompress(raw_bytes).context("Failed to decompress save data")?
        };
        info!("Decompressed to {} bytes, deserializing...", decompressed_bytes.len());
        return save_model::SaveModel::deserialize(decompressed_bytes).context("Failed to read save data")
    }
}

impl eframe::App for App {
    fn ui(&mut self, ui: &mut egui::Ui, _frame: &mut eframe::Frame) {
        #[cfg(not(target_arch = "wasm32"))]
        if ui.input_mut(|i| i.consume_key(egui::Modifiers::NONE, egui::Key::F11)) {
            let fullscreen = ui.input(|i| i.viewport().fullscreen.unwrap_or(false));
            ui.send_viewport_cmd(egui::ViewportCommand::Fullscreen(!fullscreen));
        }

        self.ui_file_drag_and_drop(ui.ctx());
        
        if let Ok(ChannelPayload { save, file_name }) = self.msg_chs.1.try_recv() {
            match save {
                Ok(save) => {
                    info!("Finding save abnormalities...");
                    self.save_abnormalities = Some(save.find_abnormalities());
                    self.save = Some(save);
                    self.world_viewer.reset();
                    self.save_loading_err = None;
                    ui.request_repaint();

                    info!("Save model is updated");

                    set_app_title(ui, format!("World Viewer - {file_name}"));
                },
                Err(e) => {
                    self.save = None;
                    self.save_loading_err = Some(e);
                    self.save_abnormalities = None;

                    info!("Error while trying to deserialize save model");

                    set_app_title(ui, "World Viewer".to_owned());
                }
            }
        }
        if ui.input_mut(|i| i.consume_shortcut(&egui::KeyboardShortcut::new(egui::Modifiers::COMMAND, egui::Key::O))) {
            self.save = None;
            self.save_loading_err = None;
            self.save_abnormalities = None;
            self.world_viewer.reset();
            
            set_app_title(ui, "World Viewer".to_owned());
        }
        if let Some(abnorms) = &self.save_abnormalities && abnorms.len() > 0 {
            egui::Window::new(RichText::new("Save Abnormalities").color(Color32::YELLOW))
                .anchor(egui::Align2::LEFT_BOTTOM, [10.0, -10.0])
                .default_open(false)
                // .frame(egui::Frame::window(&ctx.style()).fill(Color32::from_rgb(40, 40, 0)))
                .resizable([true, false])
                .vscroll(true)
                .default_height(200.0)
                .show(ui, |ui| {
                    ui.with_layout(egui::Layout::right_to_left(egui::Align::TOP), |ui| {
                        ui.add_space(5.0);
                        egui::Popup::from_toggle_button_response(&ui.small_button("ℹ")).show(|ui| {
                            ui.label("Save abnormalities indicate that the save file contains states that are impossible to achieve in the vanilla game \
                                      without external tampering with save file format. Unlike hard save loading errors, the game can still open and load \
                                      this save with abnormalities without any issues.");
                        });
                    });
                    ui.separator();
                    show_abnormalities(ui, abnorms.nodes());
                });
        }

        if self.save.is_none() {
            egui::Modal::new(egui::Id::new("open_save")).show(ui, |ui| {
                ui.set_width(300.0);
                ui.heading("📁 Open Save");
                ui.separator();

                if ui.button("Open file").clicked() {
                    let tx = self.msg_chs.0.clone();
                    let task = async move {
                        let file = rfd::AsyncFileDialog::new()
                            .add_filter("Save files", &["save"])
                            .add_filter("Uncompressed Save files", &["uncompressed-save"])
                            .pick_file()
                            .await;
                        if let Some(file_handle) = file {
                            let file_name = file_handle.file_name();
                            let raw_bytes = file_handle.read().await;
                            info!("Read {} bytes from file", raw_bytes.len());

                            let save = Self::process_save_bytes(&raw_bytes);
                            if let Err(err) = tx.send(ChannelPayload { save, file_name }) {
                                error!("Channel send failed: {err}");
                            }
                        }
                    };
                    #[cfg(target_arch = "wasm32")]
                    wasm_bindgen_futures::spawn_local(task);
                    #[cfg(not(target_arch = "wasm32"))]
                    std::thread::spawn(move || futures::executor::block_on(task));
                }
                if let Some(err) = &self.save_loading_err {
                    ui.add_space(10.0);
                    egui::Frame::group(ui.style())
                        .fill(Color32::from_rgba_premultiplied(60, 0, 0, 100))
                        .show(ui, |ui| {
                            ui.heading(RichText::new("⚠ Error").color(Color32::RED).strong());
                            ui.label(RichText::new(format!("{:#}", err)).color(Color32::RED).weak());
                        });
                }
            });
        }
        if let Some(save) = &mut self.save {
            self.world_viewer.show(ui, save);
        }
    }
}

fn show_abnormalities(ui: &mut egui::Ui, nodes: &[save_model::AbnormalitiesNode]) {
    use save_model::AbnormalitiesNode;

    for (i, node) in nodes.iter().enumerate() {
        if i > 0 {
            ui.separator();
        }
        match node {
            AbnormalitiesNode::Leaf(msg) => {
                ui.label(msg);
                // ui.colored_label(Color32::YELLOW, msg);
            }
            AbnormalitiesNode::Group { title, children } => {
                egui::CollapsingHeader::new(title)
                    .default_open(true)
                    .show(ui, |ui| {
                        show_abnormalities(ui, children);
                    });
            }
        }
    }
}

fn set_app_title(_ui: &egui::Ui, title: String) {
    info!("Changing title to {title:?}");
    #[cfg(target_arch = "wasm32")]
    {
        let window = web_sys::window().expect("no global `window` exists");
        let document = window.document().expect("should have a document on window");
        document.set_title(&title);
    }
    #[cfg(not(target_arch = "wasm32"))]
    _ui.send_viewport_cmd(egui::ViewportCommand::Title(title));
}


// fn add_number_field<T>(ui: &mut Ui, name: &'static str, value: &mut T) where T: std::str::FromStr + ToString {
//     let id = ui.auto_id_with(name);

//     let mut value_text = ui.data_mut(|data| data.remove_temp::<String>(id)).unwrap_or_else(|| value.to_string());

//     ui.label(name);
//     let response = ui.add(TextEdit::singleline(&mut value_text).id(id).clip_text(false));
//     ui.end_row();

//     if response.lost_focus() {
//         if let Ok(parsed_value) = value_text.parse::<T>() {
//             *value = parsed_value;
//         }
//     } else {
//         ui.data_mut(|data| data.insert_temp(id, value_text));
//     }
// }

// fn add_string_field(ui: &mut Ui, name: &'static str, value: &mut String) {
//     let id = ui.auto_id_with(name);
//     let is_focused = ui.memory(|mem| mem.has_focus(id));

//     let display_value = if is_focused { value } else { &mut format!("\"{value}\"") };

//     ui.label(name);
//     TextEdit::singleline(display_value).id(id).clip_text(false).ui(ui);
//     ui.end_row();
// }

// fn add_vector_field<T>(ui: &mut Ui, name: &'static str, value: &mut T) where T: FromStr + ToString {
//     let id = ui.auto_id_with(name);

//     let mut value_text = ui.data_mut(|data| data.remove_temp::<String>(id)).unwrap_or_else(|| value.to_string());
//     let display_value = if ui.memory(|mem| mem.has_focus(id)) { &mut value_text } else { &mut format!("({value_text})") };

//     ui.label(name);
//     let response = ui.add(TextEdit::singleline(display_value).id(id).clip_text(false));
//     ui.end_row();

//     if response.lost_focus() {
//         if let Ok(parsed_value) = value_text.parse::<T>() {
//             *value = parsed_value;
//         }
//     } else {
//         ui.data_mut(|data| data.insert_temp(id, value_text));
//     }
// }

// fn collapsible_section<R>(ui: &mut Ui, title: &'static str, default_open: bool, add_contents: impl FnOnce(&mut Ui) -> R) {
//     egui::CollapsingHeader::new(title)
//         .default_open(default_open)
//         .show(ui, |ui| {
//             egui::Grid::new("grid")
//             .num_columns(2)
//             .min_col_width(100.0)
//             .show(ui, add_contents);
//         });
// }

