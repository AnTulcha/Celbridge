import sys
import openai
from PIL import Image
import requests
from io import BytesIO
import tempfile
import os
import shutil


def download_image_as_png(url, file_path):
    try:
        response = requests.get(url)
        response.raise_for_status()

        # Saving the file directly to the project triggers the folder watcher
        # while the file is being saved, causing an exception because two
        # processes are attempting to access the file at the same time.
        # Saving it to a temp file and moving it into place avoids the problem.
        # A better solution would be to add a cooldown in the FolderWatcher so
        # modified files are only accessed after a delay.
        with tempfile.NamedTemporaryFile(delete=False, suffix='.png') as tmp_file:
            image = Image.open(BytesIO(response.content))
            image.save(tmp_file, format="PNG")
            tmp_file_path = tmp_file.name

        # Extract the directory from the file path
        directory = os.path.dirname(file_path)
    
        # Check if the directory exists, create it if it doesn't
        if not os.path.exists(directory):
            os.makedirs(directory)

        shutil.move(tmp_file_path, file_path)
        return True
    except Exception as e:
        print(f"Error during image download or saving: {e}")
        return False


def process_image_generation(api_key, prompt, image_file):
    try:
        # Set your OpenAI API key
        openai.api_key = api_key

        response = openai.images.generate(
            model="dall-e-3",
            prompt=prompt,
            n=1,
            size="1024x1024"
        )
        url = response.data[0].url

        if download_image_as_png(url, image_file):
            print(f"Image generated at {image_file}")
            return True
        else:
            return False
    except Exception as e:
        print(f"Error: {e}")
        return False


if __name__ == '__main__':
    if len(sys.argv) != 4:
        print("Usage: script.py <api_key> <prompt> <output_file>")
        sys.exit(1)
    else:
        api_key_arg = sys.argv[1]
        prompt_arg = sys.argv[2]
        image_file_arg = sys.argv[3]

        if process_image_generation(api_key_arg, prompt_arg, image_file_arg):
            sys.exit(0)
        else:
            sys.exit(2)
