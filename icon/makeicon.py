from PIL import Image

img_files = [
    "16.png",
    "24.png",
    "32.png",
    "48.png",
    "64.png",
    "128.png",
    "256.png",
    ]
images = [Image.open(f) for f in img_files]
images[0].save("folminder.ico", format="ICO", append_images=images[1:])
