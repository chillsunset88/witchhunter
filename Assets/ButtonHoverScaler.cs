using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] float scale = 1.08f;
    [SerializeField] float speed = 10f;
    Vector3 baseScale;

    void Awake() => baseScale = transform.localScale;

    public void OnPointerEnter(PointerEventData e)
        => StopAllCoroutines();

    public void OnPointerExit(PointerEventData e)
        => StopAllCoroutines();

    void Update()
    {
        // sederhana: skala membesar saat hover (EventSystem.referensi)
        bool hovered = RectTransformUtility.RectangleContainsScreenPoint(
            transform as RectTransform, Input.mousePosition, null);
        var target = hovered ? baseScale * scale : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * speed);
    }
}
