using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ResortEmpire.UI
{
    /// <summary>
    /// Global entry point for toast notifications. The toast canvas is loaded from
    /// <c>Resources/NotificationToast</c> on first use and survives scene loads.
    /// </summary>
    public static class Notifications
    {
        private static NotificationToast _toast;

        /// <summary>Shows a toast that closes via its × button or after its display time.</summary>
        public static void Show(string title, string message)
        {
            GetToast().Show(title, message);
        }

        /// <summary>Queues a slow-service alert while its building still needs attention.</summary>
        public static void ShowQueueWarning(string title, string message, Func<bool> stillRelevant,
            Func<bool> canMove, Action move)
        {
            if (stillRelevant())
                GetToast().ShowQueueWarning(title, message, stillRelevant, canMove, move);
        }

        private static NotificationToast GetToast()
        {
            if (_toast == null)
            {
                var canvas = Object.Instantiate(Resources.Load<GameObject>("NotificationToast"));
                Object.DontDestroyOnLoad(canvas);
                _toast = canvas.GetComponentInChildren<NotificationToast>();
            }

            return _toast;
        }
    }
}
