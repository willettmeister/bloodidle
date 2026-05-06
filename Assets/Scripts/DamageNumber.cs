using UnityEngine;
using UnityEngine.UI;

public class DamageNumber : MonoBehaviour
{
    const float Duration = 1.1f;
    const float Rise     = 90f;

    Vector2       _startPos;
    float         _elapsed;
    Text          _text;
    RectTransform _rt;

    public static void Spawn(RectTransform layer, string label, Color color, Vector2 pos)
    {
        var go = new GameObject("DmgNum");
        go.transform.SetParent(layer, false);

        var t = go.AddComponent<Text>();
        t.text      = label;
        t.color     = color;
        t.fontSize  = 40;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.5f, 0.5f);
        rt.anchorMax        = new Vector2(0.5f, 0.5f);
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = new Vector2(200, 60);
        rt.anchoredPosition = pos;

        go.AddComponent<DamageNumber>()._startPos = pos;
    }

    void Start()
    {
        _text = GetComponent<Text>();
        _rt   = GetComponent<RectTransform>();
    }

    void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / Duration;
        _rt.anchoredPosition = _startPos + Vector2.up * (Rise * t);
        var c = _text.color;
        c.a = Mathf.Clamp01(1f - t * 1.4f);
        _text.color = c;
        if (_elapsed >= Duration) Destroy(gameObject);
    }
}
