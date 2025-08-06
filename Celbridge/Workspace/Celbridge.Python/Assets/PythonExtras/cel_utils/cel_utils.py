import os
import runpy
import traceback

class CelUtils:
    """
    Celbridge utility functions available under the `cel.` namespace.
    """

    def run(self, script_path: str):
        """
        Executes a Python script in the opened project.

        Usage:
            cel.run("myscript.py")
        """
        abs_path = os.path.abspath(script_path)

        if not os.path.isfile(abs_path):
            print(f" Script not found: {script_path}")
            return

        try:
            runpy.run_path(abs_path, run_name="__main__")

        except SystemExit as e:
            print(f"Script called sys.exit() with code: {e.code}")

        except Exception:
            print(f"Error while running script:")
            traceback.print_exc()
