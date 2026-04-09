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

    _ = eframe::WebLogger::init(log::LevelFilter::Debug);
    std::panic::set_hook(Box::new(console_error_panic_hook::hook));

    let web_options = eframe::WebOptions::default();

    wasm_bindgen_futures::spawn_local(async {
        let document = web_sys::window()
            .expect("No window")
            .document()
            .expect("No document");

        let canvas = document
            .get_element_by_id("the_canvas_id")
            .expect("Failed to find the_canvas_id")
            .dyn_into::<web_sys::HtmlCanvasElement>()
            .expect("the_canvas_id was not a HtmlCanvasElement");

        let start_res = eframe::WebRunner::new()
            .start(canvas, web_options, Box::new(|cc| Ok(Box::new(app::App::new(cc)))))
            .await;
        
        if let Some(loading_text) = document.get_element_by_id("loading_text") {
            match start_res {
                Ok(_) => {
                    loading_text.remove();
                },
                Err(e) => {
                    let error_msg = e.as_string().unwrap_or_else(|| format!("{:?}", e));
                    loading_text.set_inner_html(&format!(r#"
                        <p style="color: #ff6b6b; font-weight: bold;">Failed to load the app.</p>
                        <p style="font-size: 14px; font-family: monospace;">{error_msg}</p>
                        <p style="font-size: 14px;">Make sure you use a modern browser with WebGL and WASM enabled.</p>
                        <p style="font-size: 14px;">Check the console for details.</p>
                    "#));
                    web_sys::console::error_1(&format!("start error: {error_msg}").into());
                },
            }
        }
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