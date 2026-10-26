using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Shared UI factory: fonts, colors, rounded panels, buttons, labels.</summary>
public static class UiTheme
{
    public static readonly Color Navy = new Color(0.14f, 0.16f, 0.27f);
    public static readonly Color Cream = new Color(0.99f, 0.97f, 0.92f);
    public static readonly Color Amber = new Color(0.94f, 0.65f, 0.24f);
    public static readonly Color Green = new Color(0.30f, 0.73f, 0.41f);
    public static readonly Color Red = new Color(0.88f, 0.38f, 0.36f);
    public static readonly Color Grey = new Color(0.59f, 0.61f, 0.69f);
    public static readonly Color Gold = new Color(1f, 0.78f, 0.24f);

    private static Font font;
    public static Font Font => font != null ? font
        : font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    public static Image CreateImage(Transform parent, string name, string sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = ProceduralSprites.Get(sprite);
        img.color = color;
        img.type = sprite.StartsWith("uiPanel") || sprite.StartsWith("uiButton") ? Image.Type.Sliced : Image.Type.Simple;
        return img;
    }

    public static RectTransform CreatePanel(Transform parent, string name, string sprite, Color color)
    {
        var img = CreateImage(parent, name, sprite, color);
        return (RectTransform)img.transform;
    }

    public static Text CreateText(Transform parent, string name, string content, int size,
                                  Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = Font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Text CreateOutlinedText(Transform parent, string name, string content, int size,
                                          Color color, TextAnchor align = TextAnchor.MiddleCenter)
    {
        var t = CreateText(parent, name, content, size, color, align);
        var outline = t.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        var shadow = t.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, 4f);
        return t;
    }

    public static Button CreateButton(Transform parent, string name, string label, string sprite,
                                      int fontSize, Color textColor, UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = ProceduralSprites.Get(sprite);
        img.type = Image.Type.Sliced;

        var btn = go.GetComponent<Button>();
        var tint = img.color;
        var colors = btn.colors;
        colors.highlightedColor = new Color(tint.r * 1.1f, tint.g * 1.1f, tint.b * 1.1f, 1f);
        colors.pressedColor = new Color(tint.r * 0.85f, tint.g * 0.85f, tint.b * 0.85f, 1f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        btn.colors = colors;

        if (label != null)
        {
            var t = CreateText(go.transform, "Label", label, fontSize, textColor);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        btn.onClick.AddListener(() =>
        {
            ProceduralAudio.PlaySfx("click", 0.7f);
            // press-pop animation
            var go2 = btn.gameObject;
            Tween.Scale(go2.transform, go2.transform.localScale, go2.transform.localScale * 0.92f, 0.05f,
                Tween.Linear, () => Tween.Scale(go2.transform, go2.transform.localScale, Vector3.one, 0.08f));
            onClick?.Invoke();
        });
        return btn;
    }

    /// <summary>
    /// Unity 6 UGUI renders runtime-created sliced sprites unreliably, so after layout we
    /// swap every sliced image to an exact-size white rounded texture tinted by Image.color.
    /// </summary>
    public static void FinalizePanelImages(Canvas canvas)
    {
        foreach (var img in canvas.GetComponentsInChildren<Image>(true))
        {
            if (img.sprite == null || img.type != Image.Type.Sliced) continue;
            if (img.sprite.name == "uiCircle") continue; // circles render fine as-is
            if (!ProceduralSprites.TryGetPanelColor(img.sprite, out var color)) continue;
            var rt = (RectTransform)img.transform;
            int w = Mathf.Max(8, Mathf.RoundToInt(rt.rect.width));
            int h = Mathf.Max(8, Mathf.RoundToInt(rt.rect.height));
            int radius = Mathf.Min(30, Mathf.Min(w, h) / 3);
            img.sprite = ProceduralSprites.RoundedWhite(w, h, radius);
            img.type = Image.Type.Simple;
            img.color = color;
        }
    }

    public static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
                               Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        // pivot matches the anchor so `pos` is the offset of that corner/edge from the anchor point
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }
}
