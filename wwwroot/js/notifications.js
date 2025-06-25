// wwwroot/js/notifications.js
class NotificationManager {
  constructor() {
    this.connection = null;
    this.isInitialized = false;
    this.initializeSignalR();
  }

  async initializeSignalR() {
    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .withAutomaticReconnect()
        .build();

      // Handle incoming notifications
      this.connection.on("ReceiveNotification", (notification) => {
        console.log("Received notification:", notification);
        this.showToast(notification);
        this.updateNotificationBell();
      });

      // Handle unread count updates
      this.connection.on("UnreadCountUpdated", (count) => {
        console.log("Unread count updated:", count);
        this.updateBadge(count);
      });

      // Connection events
      this.connection.onreconnecting(() => {
        console.log("SignalR reconnecting...");
      });

      this.connection.onreconnected(() => {
        console.log("SignalR reconnected");
      });

      this.connection.onclose(() => {
        console.log("SignalR connection closed");
      });

      await this.connection.start();
      console.log("SignalR Connected successfully");
      this.isInitialized = true;

      // Load initial data
      await this.updateNotificationBell();
    } catch (err) {
      console.error("SignalR Connection Error: ", err);
      // Retry after 5 seconds
      setTimeout(() => this.initializeSignalR(), 5000);
    }
  }

  showToast(notification) {
    // Create toast HTML
    const toastId = "toast-" + Date.now();
    const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white bg-primary border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <strong>${this.escapeHtml(
                          notification.title
                        )}</strong><br>
                        <small>${this.escapeHtml(notification.message)}</small>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

    // Get or create toast container
    let toastContainer = document.getElementById("toast-container");
    if (!toastContainer) {
      toastContainer = document.createElement("div");
      toastContainer.id = "toast-container";
      toastContainer.className =
        "toast-container position-fixed bottom-0 end-0 p-3";
      toastContainer.style.zIndex = "1060";
      document.body.appendChild(toastContainer);
    }

    // Add toast to container
    toastContainer.insertAdjacentHTML("beforeend", toastHtml);

    // Show the toast
    const toastElement = document.getElementById(toastId);
    const toast = new bootstrap.Toast(toastElement, {
      autohide: true,
      delay: 5000,
    });

    toast.show();

    // Remove element after it's hidden
    toastElement.addEventListener("hidden.bs.toast", () => {
      toastElement.remove();
    });

    // Play notification sound (optional)
    this.playNotificationSound();
  }

  async updateNotificationBell() {
    try {
      // Update unread count
      const countResponse = await fetch("/Notifications/GetUnreadCount");
      const countData = await countResponse.json();
      this.updateBadge(countData.count);

      // Update notification dropdown if it exists
      await this.loadRecentNotifications();
    } catch (error) {
      console.error("Error updating notification bell:", error);
    }
  }

  updateBadge(count) {
    const badge = document.getElementById("notification-badge");
    if (badge) {
      if (count > 0) {
        badge.textContent = count > 99 ? "99+" : count;
        badge.style.display = "inline-block";
        badge.className =
          "position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger";
      } else {
        badge.style.display = "none";
      }
    }
  }

  async loadRecentNotifications() {
    try {
      const response = await fetch("/Notifications/GetRecent");
      const notifications = await response.json();
      this.renderNotificationDropdown(notifications);
    } catch (error) {
      console.error("Error loading recent notifications:", error);
    }
  }

  renderNotificationDropdown(notifications) {
    const dropdown = document.getElementById("notification-dropdown");
    if (!dropdown) return;

    if (notifications.length === 0) {
      dropdown.innerHTML = `
                <li><span class="dropdown-item-text text-muted text-center py-3">
                    <i class="fas fa-bell-slash fa-2x mb-2 d-block"></i>
                    No notifications
                </span></li>
            `;
      return;
    }

    const html = notifications
      .map(
        (notification) => `
            <li>
                <a class="dropdown-item ${
                  !notification.isRead ? "bg-light" : ""
                }" 
                   href="#" onclick="notificationManager.markAsRead(${
                     notification.id
                   }, event)">
                    <div class="d-flex justify-content-between align-items-start">
                        <div class="flex-grow-1">
                            <h6 class="mb-1 fw-bold">${this.escapeHtml(
                              notification.title
                            )}</h6>
                            <p class="mb-1 small text-muted">${this.escapeHtml(
                              notification.message
                            )}</p>
                            <small class="text-muted">${this.formatDate(
                              notification.createdAt
                            )}</small>
                        </div>
                        ${
                          !notification.isRead
                            ? '<span class="badge bg-primary ms-2">New</span>'
                            : ""
                        }
                    </div>
                </a>
            </li>
        `
      )
      .join("");

    dropdown.innerHTML = html;
  }

  async markAsRead(notificationId, event) {
    if (event) {
      event.preventDefault();
    }

    try {
      const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
      )?.value;

      const response = await fetch("/Notifications/MarkAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: new URLSearchParams({
          id: notificationId,
          __RequestVerificationToken: token || "",
        }),
      });

      if (response.ok) {
        // The SignalR connection will handle updating the UI
        console.log("Notification marked as read");
      }
    } catch (error) {
      console.error("Error marking notification as read:", error);
    }
  }

  async markAllAsRead() {
    try {
      const token = document.querySelector(
        'input[name="__RequestVerificationToken"]'
      )?.value;

      const response = await fetch("/Notifications/MarkAllAsRead", {
        method: "POST",
        headers: {
          "Content-Type": "application/x-www-form-urlencoded",
        },
        body: new URLSearchParams({
          __RequestVerificationToken: token || "",
        }),
      });

      if (response.ok) {
        console.log("All notifications marked as read");
        await this.updateNotificationBell();
      }
    } catch (error) {
      console.error("Error marking all notifications as read:", error);
    }
  }

  playNotificationSound() {
    // Create and play a subtle notification sound
    if ("AudioContext" in window || "webkitAudioContext" in window) {
      try {
        const audioContext = new (window.AudioContext ||
          window.webkitAudioContext)();
        const oscillator = audioContext.createOscillator();
        const gainNode = audioContext.createGain();

        oscillator.connect(gainNode);
        gainNode.connect(audioContext.destination);

        oscillator.frequency.setValueAtTime(800, audioContext.currentTime);
        oscillator.frequency.exponentialRampToValueAtTime(
          400,
          audioContext.currentTime + 0.1
        );

        gainNode.gain.setValueAtTime(0.1, audioContext.currentTime);
        gainNode.gain.exponentialRampToValueAtTime(
          0.01,
          audioContext.currentTime + 0.1
        );

        oscillator.start(audioContext.currentTime);
        oscillator.stop(audioContext.currentTime + 0.1);
      } catch (e) {
        // Silently fail if audio context is not available
      }
    }
  }

  escapeHtml(text) {
    const map = {
      "&": "&amp;",
      "<": "&lt;",
      ">": "&gt;",
      '"': "&quot;",
      "'": "&#039;",
    };
    return text.replace(/[&<>"']/g, (m) => map[m]);
  }

  formatDate(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return "Just now";
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  }
}

// Initialize notification manager when page loads
document.addEventListener("DOMContentLoaded", function () {
  if (typeof signalR !== "undefined") {
    window.notificationManager = new NotificationManager();
  } else {
    console.warn("SignalR not loaded. Real-time notifications will not work.");
  }
});
