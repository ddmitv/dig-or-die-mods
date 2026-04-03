#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")] // hide console window on Windows in release

mod app;
mod misc;
mod save_model;
mod world_view;
mod sprites_info;

#[cfg(not(target_arch = "wasm32"))]  
fn main() -> eframe::Result {
    env_logger::init(); // Log to stderr (if you run with `RUST_LOG=debug`).

    let options = eframe::NativeOptions {
        viewport: egui::ViewportBuilder::default()
            .with_icon(load_app_icon()),
        ..Default::default()
    };
    return eframe::run_native(
        "World Viewer",
        options,
        Box::new(|cc| Ok(Box::new(app::App::new(cc)))),
    );
}

#[cfg(target_arch = "wasm32")]
fn main() {  
    use eframe::wasm_bindgen::JsCast;

    eframe::WebLogger::init(log::LevelFilter::Debug).ok();
    std::panic::set_hook(Box::new(console_error_panic_hook::hook));

    let web_options = eframe::WebOptions::default();

    wasm_bindgen_futures::spawn_local(async {
        let document = web_sys::window().expect("No window").document().expect("No document");

        let canvas: web_sys::HtmlCanvasElement =
            document.get_element_by_id("the_canvas_id").expect("Failed to find the_canvas_id")
            .dyn_into().expect("the_canvas_id was not a HtmlCanvasElement");

        eframe::WebRunner::new()
            .start(canvas, web_options, Box::new(|cc| Ok(Box::new(app::App::new(cc)))))
            .await
            .expect("failed to start eframe");
    });
}

#[cfg(not(target_arch = "wasm32"))]
fn load_app_icon() -> egui::IconData {
    let bytes = include_bytes!("../assets/world-viewer-logo.png");
    let image = image::load_from_memory_with_format(bytes, image::ImageFormat::Png)
        .expect("Failed to load icon app image")
        .into_rgba8();
    let (width, height) = image.dimensions();

    return egui::IconData { rgba: image.into_vec(), width, height }
}