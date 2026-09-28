import os
from pathlib import Path

# Target current working directory directly
ROOT_DIR = Path.cwd()

structure = [
    # Root level files
    "App.xaml",
    "App.xaml.cs",
    "MainWindow.xaml",
    "MainWindow.xaml.cs",
    
    # Binaries directory
    "Binaries/adb.exe",
    "Binaries/fastboot.exe",
    "Binaries/AdbWinApi.dll",
    "Binaries/AdbWinUsbApi.dll",
    
    # Core handlers
    "Core/AdbManager.cs",
    "Core/FastbootManager.cs",
    "Core/SerialManager.cs",
    "Core/DeviceMonitor.cs",
    
    # Brand handlers
    "Handlers/SamsungHandler.cs",
    "Handlers/XiaomiHandler.cs",
    "Handlers/RealmeHandler.cs",
    "Handlers/GenericAndroidHandler.cs",
    
    # Models
    "Models/ConnectedDevice.cs",
    "Models/OperationLog.cs",
    
    # Utilities
    "Utilities/Logger.cs",
    "Utilities/ArchiveExtractor.cs"
]

def setup_cyberforge_studio():
    print("=" * 60)
    print(f" BUILDING PROJECT STRUCTURE IN: {ROOT_DIR}")
    print("=" * 60)
    
    created_folders = set()
    created_files = 0

    for relative_path in structure:
        full_path = ROOT_DIR / relative_path
        
        # Ensure parent subdirectories exist
        parent_dir = full_path.parent
        if parent_dir not in created_folders and parent_dir != ROOT_DIR:
            os.makedirs(parent_dir, exist_ok=True)
            created_folders.add(parent_dir)
            print(f"[DIR CREATED]  {parent_dir.relative_to(ROOT_DIR)}")
        
        # Create empty file if missing
        if not full_path.exists():
            full_path.touch()
            created_files += 1
            print(f"[FILE CREATED] {relative_path}")
        else:
            print(f"[EXISTS]       {relative_path}")

    print("-" * 60)
    print(f"Done! Generated subdirectories and {created_files} files inside CyberForge Studio.")
    print("=" * 60)

if __name__ == "__main__":
    setup_cyberforge_studio()