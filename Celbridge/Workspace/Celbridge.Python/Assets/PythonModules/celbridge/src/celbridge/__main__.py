import os, sys
import platform

from . import customize_ipython
from . import exit_lock


def _main():

    # Add project dir to the path so we can easily import from Python scripts in the project
    project_dir = os.getcwd()
    if project_dir not in sys.path:
        sys.path.insert(0, project_dir)

    # Prevent the user from exiting the interpreter in Celbridge
    exit_lock.apply_exit_lock()

    # Customize the appearance of the IPython REPL
    customize_ipython.apply_ipython_customizations()

    # Display version numbers in banner
    celbridge_version = os.environ.get('CELBRIDGE_VERSION', '')
    python_version = platform.python_version()

    # Clear the console output
    os.system('cls' if os.name == 'nt' else 'clear')

    print(f"Celbridge v{celbridge_version} - Python v{python_version}")


if __name__ == "__main__":
    _main()
