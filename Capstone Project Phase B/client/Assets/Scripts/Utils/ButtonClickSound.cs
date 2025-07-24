using LoM.Super;
using UnityEngine.UI;

public class ButtonClickSound : SuperBehaviour {
    private Button _button; // Reference to the button component

    /**
     * <summary>Initializes the component and sets up an event listener.</summary>
     */
    private void Awake() {
        _button = GetComponent<Button>();
        _button?.onClick.AddListener(() => AudioManager.Instance.PlayClickSoundEffect());
    }
    
    /**
     * <summary>Removes the event listener when the component is destroyed.</summary>
     */
    private void OnDestroy() {
        _button?.onClick.RemoveListener(() => AudioManager.Instance.PlayClickSoundEffect());
    }
}