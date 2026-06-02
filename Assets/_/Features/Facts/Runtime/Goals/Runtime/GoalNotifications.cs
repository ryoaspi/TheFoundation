using UnityEngine;
using TMPro;

namespace TheFundation.Runtime
{
    public class GoalNotifications : MonoBehaviour
    {
        #region Publics

        [Header("UI")]
        public TMP_Text m_NotificationText;
        public float m_DisplayDuration = 2f;

        #endregion

        #region Private and Protected

        private float _timer = 0f;
        private bool _visible = false;

        #endregion


        #region API Unity

        private void OnEnable()
        {
            GoalEvents.OnGoalCompleted += _OnGoalCompleted;
        }

        private void OnDisable()
        {
            GoalEvents.OnGoalCompleted -= _OnGoalCompleted;
        }

        private void Update()
        {
            if (!_visible)
                return;

            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                _Hide();
            }
        }

        #endregion


        #region Main Methods (méthodes private)

        private void _OnGoalCompleted(string key)
        {
            if (m_NotificationText == null)
                return;

            m_NotificationText.text = $"Objectif complété : {key}";
            _Show();
        }

        private void _Show()
        {
            _visible = true;
            _timer = m_DisplayDuration;
            m_NotificationText.gameObject.SetActive(true);
        }

        private void _Hide()
        {
            _visible = false;
            m_NotificationText.gameObject.SetActive(false);
        }

        #endregion
    }
}