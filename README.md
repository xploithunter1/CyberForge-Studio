XploitForge/
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml                   <-- Primary UI layout
├── MainWindow.xaml.cs                <-- UI Event bindings & Async hooks
│
├── Binaries/                         <-- External executables (Copy to Output)
│   ├── adb.exe
│   ├── fastboot.exe
│   ├── AdbWinApi.dll
│   └── AdbWinUsbApi.dll
│
├── Core/                             <-- Low-Level Hardware & Communication Protocols
│   ├── AdbManager.cs                 <-- ADB Execution Engine
│   ├── FastbootManager.cs            <-- Fastboot Flashing & Partition Handler
│   ├── SerialManager.cs              <-- COM Port & AT Command Interface
│   └── DeviceMonitor.cs              <-- USB Hotplug & Auto-Detection
│
├── Handlers/                         <-- Brand-Specific Modules
│   ├── SamsungHandler.cs             <-- MTP / AT FRP / Odin Flash Logic
│   ├── XiaomiHandler.cs              <-- Fastboot / Mi Flash / EDL Flashing
│   ├── RealmeHandler.cs              <-- Realme/Oppo Fastboot & MTK Protocols
│   └── GenericAndroidHandler.cs      <-- Universal ADB / Sideload commands
│
├── Models/                           <-- Data Structures & View Contracts
│   ├── ConnectedDevice.cs            <-- Device metadata (Serial, Mode, Port)
│   └── OperationLog.cs               <-- Real-time UI log entry formatting
│
└── Utilities/                        <-- Shared Helpers
    ├── Logger.cs                     <-- Thread-safe UI Logger
    └── ArchiveExtractor.cs           <-- Firmware parsing (.tar, .zip, .img)