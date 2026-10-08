# ShinonoDNSUnlocker (SDU)

**A small Windows network utility for when Steam decides to play hide-and-seek.**

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows-lightgray.svg)]()
[![License](https://img.shields.io/badge/License-MIT-green.svg)]()

Choose your language: [Tiếng Việt](#tiếng-việt) | [English](#english)

---

## English

### Introduction

**ShinonoDNSUnlocker (SDU)** is an open-source **WPF/C#** Windows network utility built around a simple idea: make DNS management less annoying.

SDU is primarily designed to help with Steam connectivity when DNS-related issues or ISP-level network restrictions get in the way. It also provides convenient controls for DNS, DNS over HTTPS, connectivity testing, and restoring network settings.

In other words: **fewer Settings windows, fewer clicks, fewer opportunities to wonder what you just changed.**

### Features

- **Quick DNS Switching:** Automatically configures IPv4 and IPv6 using Cloudflare (`1.1.1.1`) or Google DNS (`8.8.8.8`).
- **DNS over HTTPS (DoH):** Supports system-level encrypted DNS queries.
- **Steam Hosts Bypass:** As a fallback, SDU can use the Windows `hosts` file to map Steam domains according to the tool's configuration.
- **DHCP Reset:** Restores the selected network adapter to automatic DNS configuration and removes custom settings.
- **Auto Ping:** Periodically monitors network latency so you can tell whether the connection is healthy or simply having a moment.
- **Steam Connectivity Test:** Uses HTTP requests to check whether Steam domains are reachable.
- **Light/Dark Mode:** Because staring at a white Settings window at 2 AM is not a networking requirement.
- **Bilingual UI:** English and Vietnamese support.
- **Automatic Settings Save:** Remembers your language, theme, and selected network adapter.

### System Requirements

- **OS:** Windows 10 or Windows 11.
- **Privileges:** Administrator.
- **Runtime:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### Build from Source

If you prefer compiling the project yourself:

1. Clone this repository.
2. Open `ShinonoDNSUnlocker.sln` with **Visual Studio 2022**.
3. Restore the project's NuGet packages. If required by the project, install `MaterialDesignThemes` through NuGet Package Manager.
4. Select the **Release** configuration.
5. Press `Ctrl + Shift + B`.
6. The build output will be generated under the corresponding `bin/Release/net8.0-windows/` directory.

### Disclaimer

SDU can modify Windows network adapter settings and the system `hosts` file, so Administrator privileges are required.

Use the tool responsibly. If you are not sure what a setting does, do not enable it just because it looks impressive. Your network configuration has suffered enough already.

---
## Tiếng Việt

### Giới thiệu

**ShinonoDNSUnlocker (SDU)** là một công cụ mã nguồn mở viết bằng **WPF/C#**, được tạo ra với một mục tiêu khá đơn giản: giúp bạn quản lý DNS trên Windows mà không phải mở một đống cửa sổ Settings rồi tự hỏi mình vừa bấm vào đâu.

SDU tập trung vào việc hỗ trợ truy cập các dịch vụ của **Steam** trong những trường hợp kết nối bị ảnh hưởng bởi DNS hoặc một số cơ chế chặn mạng của ISP. Ngoài ra, nó cũng cung cấp các chức năng DNS, DoH, kiểm tra kết nối và khôi phục cấu hình mạng trong một giao diện duy nhất.

Nói ngắn gọn: **ít cửa sổ hơn, ít thao tác hơn, và ít cơ hội bấm nhầm hơn.**

### Tính năng

- **Đổi DNS nhanh:** Tự động cấu hình IPv4 và IPv6 với Cloudflare (`1.1.1.1`) hoặc Google DNS (`8.8.8.8`).
- **DNS over HTTPS (DoH):** Hỗ trợ bật DoH để mã hóa các truy vấn DNS ở cấp hệ thống.
- **Steam Hosts Bypass:** Khi đổi DNS chưa đủ, SDU có thể sử dụng file `hosts` của Windows để ánh xạ các domain Steam theo cấu hình của tool.
- **Khôi phục DHCP:** Đưa adapter mạng về chế độ nhận DNS tự động từ DHCP và dọn các cấu hình tùy chỉnh.
- **Auto Ping:** Theo dõi độ trễ kết nối định kỳ để bạn biết mạng đang ổn hay đang "suy nghĩ".
- **Steam Connectivity Test:** Kiểm tra khả năng kết nối tới các domain Steam bằng HTTP.
- **Light/Dark Mode:** Vì nhìn màn hình trắng lúc 2 giờ sáng không phải là một chiến lược mạng.
- **Song ngữ:** Hỗ trợ tiếng Việt và tiếng Anh.
- **Tự động lưu thiết lập:** Ghi nhớ ngôn ngữ, giao diện và network adapter đã chọn.

### Yêu cầu hệ thống

- **OS:** Windows 10 hoặc Windows 11.
- **Quyền:** Administrator.
- **Runtime:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

### Build từ Source Code

Nếu bạn muốn tự build thay vì tải bản release:

1. Clone repository về máy.
2. Mở `ShinonoDNSUnlocker.sln` bằng **Visual Studio 2022**.
3. Khôi phục các NuGet package của project. Nếu project yêu cầu, cài `MaterialDesignThemes` từ NuGet Package Manager.
4. Chọn configuration **Release**.
5. Nhấn `Ctrl + Shift + B`.
6. File build sẽ xuất hiện trong thư mục tương ứng dưới `bin/Release/net8.0-windows/`.

### Lưu ý

SDU có thể thay đổi cấu hình network adapter và file hệ thống `hosts`, vì vậy chương trình yêu cầu quyền Administrator.

Nếu bạn không biết một tùy chọn đang làm gì, đừng bật nó chỉ vì nó có vẻ "pro". Máy tính thường không thưởng cho sự tò mò.

---
## License

This project is released under the **MIT License**.
