using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TheFundation.Runtime
{
    public class SaveSlotsPanel : MonoBehaviour
    {
        [SerializeField] private Transform container;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private int maxSlots = 10;

        void Start() => Build();

        void Build()
        {
            foreach (Transform c in container) Destroy(c.gameObject);
            for (int i = 0; i < maxSlots; i++)
            {
                var go = Instantiate(slotPrefab, container);
                go.name = $"Slot {i}";
                SetupSlot(go, i);
            }
        }

        void SetupSlot(GameObject go, int slot)
        {
            var label  = go.transform.Find("Label").GetComponent<TMP_Text>();
            var bLoad  = go.transform.Find("BtnLoad").GetComponent<Button>();
            var bSave  = go.transform.Find("BtnSave").GetComponent<Button>();
            var bDel   = go.transform.Find("BtnDelete").GetComponent<Button>();

            void Refresh()
            {
                bool exists = GameManager.HasSaveInSlot(slot);
                string existsText = LocalizationManager.GetText(exists ? "exists" : "empty");
                label.text = string.Format(LocalizationManager.GetText("slot_exists"), slot + 1, existsText);

                bLoad.GetComponentInChildren<TMP_Text>().text = LocalizationManager.GetText("load");
                bSave.GetComponentInChildren<TMP_Text>().text = LocalizationManager.GetText("save");
                bDel.GetComponentInChildren<TMP_Text>().text  = LocalizationManager.GetText("delete");

                bLoad.interactable = exists;
                bDel.interactable  = exists;
            }

            bLoad.onClick.AddListener(() => { GameManager.LoadFromSlot(slot);  Refresh(); });
            bSave.onClick.AddListener(() => { GameManager.SaveToSlot(slot);   Refresh(); });
            bDel.onClick.AddListener (() => { GameManager.DeleteSlot(slot);   Refresh(); });

            Refresh();
        }
    }
}
