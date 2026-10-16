# OLT Secure Browser 🛡️
**Online Test Environment Lockdown System**

OLT Secure Browser is an enterprise-grade, Kiosk-mode desktop application built for Windows. It is designed to provide a 100% secure and cheat-proof environment for online examinations by strictly locking down the system.


## 🚀 Key Features :-

* **Strict Screen Lockdown:** Forces a borderless, top-most full-screen window. Suspends Windows Explorer to block Taskbar access and 3-finger touchpad gestures.
* **Anti-Cheating Keyboard Hooks:** Intercepts low-level system shortcuts (Win32 APIs). Completely blocks `Alt+Tab`, `Alt+F4`, `Windows Key`, `Ctrl+C`, `Copilot key`, `Ctrl+V`, `PrintScreen`, and `Escape`.
* **Background Process Killer:** Actively monitors and terminates forbidden background applications like Discord, Telegram, OBS Studio, AnyDesk, and Task Manager.
* **Smart Network Monitor:** Continuously checks for internet connectivity. Instantly blacks out the screen and locks the environment if the network disconnects, preventing offline cheating.
* **Dynamic Cloud Configuration:** Authentication passkeys, exam target URLs, and version updates are fetched remotely and asynchronously via GitHub Gist, requiring zero hardcoded updates.


## 💻 Technical Stack :-

* **Language:** C#
* **Framework:** Windows Forms (.NET Framework)
* **Browser Engine:** Microsoft Edge WebView2 Runtime
* **Low-Level Controls:** `user32.dll` and `kernel32.dll` (Win32 APIs)


## 👨‍💻 Developer Details :-

* **Name:** Anuj Tandon
* **Course:** BCA 2nd Year
* **Section:** B
* **Institution:** Pranveer Singh Institute of Technology (PSIT), Kanpur
* **Version:** 1.0 (Enterprise Edition)

---
*Note: This project is strictly for educational purposes and academic demonstration.*
