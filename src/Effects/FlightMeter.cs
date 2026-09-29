using FlightState = BloodbugMode.BloodbugController.FlightState;
using UnityEngine;
using UnityEngine.UI;

namespace BloodbugMode
{
    internal class FlightMeter
    {
        private const float Width = 190f;
        private const float Height = 24f;
        private const float IconSize = 24f;
        private const float BarHeight = 14f;
        private const float Border = 2f;

        private static readonly Color Backing = new Color(0f, 0f, 0f, 0.75f);
        private static readonly Color Flying = new Color32(193, 48, 48, 255);
        private static readonly Color Recovering = new Color32(240, 97, 97, 255);
        private static readonly Color Spent = new Color32(107, 107, 107, 255);
        private static readonly Color Pilled = new Color(0f, 0.48f, 1f);

        private GameObject root;
        private RectTransform fill;
        private Image fillImage;
        private float punch;

        // https://docs.unity3d.com/ScriptReference/RectTransform.html
        public static FlightMeter Create(Transform parent)
        {
            var meter = new FlightMeter();
            meter.root = NewElement("BloodbugFlightMeter", parent, out RectTransform rootRect);
            rootRect.sizeDelta = new Vector2(Width, Height);

            Image icon = NewImage("Icon", rootRect, BloodbugContent.Icon, Color.white, out RectTransform iconRect);
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(IconSize, IconSize);
            icon.preserveAspect = true;

            NewImage("Backing", rootRect, BloodbugContent.Pixel, Backing, out RectTransform backRect);
            backRect.anchorMin = new Vector2(0f, 0.5f);
            backRect.anchorMax = new Vector2(1f, 0.5f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.offsetMin = new Vector2(IconSize + 6f, -BarHeight / 2f);
            backRect.offsetMax = new Vector2(0f, BarHeight / 2f);

            meter.fillImage = NewImage("Fill", backRect, BloodbugContent.Pixel, Flying, out meter.fill);
            meter.fill.anchorMin = Vector2.zero;
            meter.fill.anchorMax = Vector2.one;
            meter.fill.offsetMin = new Vector2(Border, Border);
            meter.fill.offsetMax = new Vector2(-Border, -Border);
            return meter;
        }

        public void Set(float fraction, FlightState state, bool tooLowToFly, bool onPills, bool feeding)
        {
            fraction = Mathf.Clamp01(fraction);
            fill.anchorMax = new Vector2(fraction, 1f);
            fill.offsetMax = new Vector2(-Border * fraction, -Border);
            fillImage.color = ColorFor(fraction, state, tooLowToFly, onPills, feeding);

            punch = Mathf.Lerp(punch, 0f, Time.unscaledDeltaTime * 8f);
            root.transform.localScale = Vector3.one * (1f + punch);
        }

        public void Punch()
        {
            punch = 0.25f;
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);
        }

        public void Destroy()
        {
            Object.Destroy(root);
        }

        private static Color ColorFor(float fraction, FlightState state, bool tooLowToFly, bool onPills, bool feeding)
        {
            if (onPills) return Pulse(Pilled, 1.5f, 0.25f);
            if (feeding) return Pulse(Recovering, 3f, 0.4f);
            if (tooLowToFly) return Spent;
            if (state != FlightState.Flying) return Recovering;
            return fraction < 0.25f ? Pulse(Flying, 4f, 0.6f) : Flying;
        }

        private static Color Pulse(Color color, float rate, float amount)
        {
            return Color.Lerp(color, Color.white, Mathf.PingPong(Time.unscaledTime * rate, 1f) * amount);
        }

        private static GameObject NewElement(string name, Transform parent, out RectTransform rect)
        {
            var element = new GameObject(name, typeof(RectTransform));
            element.layer = parent.gameObject.layer;
            rect = (RectTransform)element.transform;
            rect.SetParent(parent, false);
            return element;
        }

        private static Image NewImage(string name, Transform parent, Sprite sprite, Color color, out RectTransform rect)
        {
            Image image = NewElement(name, parent, out rect).AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }
}
