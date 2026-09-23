using UnityEngine;
using Verse;

namespace UnitedFront.UI
{
    public sealed class ColorPicker
    {
        private const float Pad = 10f;
        private const float HueBarWidth = 15f;
        private const float MinGap = 12f;
        private const float MarkerSize = 14f;

        private static readonly Color DarkMarker = new Color(0.06f, 0.06f, 0.06f);

        private static Texture2D _colorChart;
        private static Texture2D _hueChart;

        private bool _draggingHue;
        private bool _draggingSquare;

        public static float WidthFor(float height) => height + HueBarWidth + MinGap;

        public static float HeightFor(float width) => width - HueBarWidth - MinGap;

        private static Texture2D HueChart
        {
            get
            {
                if (_hueChart == null) BuildCharts();
                return _hueChart;
            }
        }

        private static Texture2D ColorChart
        {
            get
            {
                if (_colorChart == null) BuildCharts();
                return _colorChart;
            }
        }

        private static void BuildCharts()
        {
            _hueChart = new Texture2D(1, 255);
            for (int i = 0; i < 255; i++)
                _hueChart.SetPixel(0, i, Color.HSVToRGB(Mathf.InverseLerp(0f, 255f, i), 1f, 1f));
            _hueChart.Apply(false);

            _colorChart = new Texture2D(255, 255);
            for (int x = 0; x < 255; x++)
            {
                Color sat = Color.Lerp(Color.clear, Color.white, Mathf.InverseLerp(0f, 255f, x));
                for (int y = 0; y < 255; y++)
                    _colorChart.SetPixel(x, y, Color32.Lerp(Color.black, sat, Mathf.InverseLerp(0f, 255f, y)));
            }
            _colorChart.Apply(false);
        }

        public bool Draw(Rect fullRect, ref float hue, ref float saturation, ref float value)
        {
            Event e = Event.current;
            Rect inner = fullRect.ContractedBy(Pad);

            Rect hueRect = new Rect(inner.x, inner.y, HueBarWidth, inner.height);
            float side = Mathf.Min(inner.height, inner.width - HueBarWidth - MinGap);
            Rect squareRect = new Rect(inner.xMax - side, inner.y, side, side);

            if (e.rawType == EventType.MouseUp)
            {
                _draggingHue = false;
                _draggingSquare = false;
            }

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (Mouse.IsOver(hueRect)) _draggingHue = true;
                else if (Mouse.IsOver(squareRect)) _draggingSquare = true;
            }

            bool changed = false;

            if (_draggingHue && e.isMouse)
            {
                float h = Mathf.InverseLerp(hueRect.yMax, hueRect.y, e.mousePosition.y);
                if (!Mathf.Approximately(h, hue))
                {
                    hue = h;
                    changed = true;
                }
                e.Use();
            }
            else if (_draggingSquare && e.isMouse)
            {
                float s = Mathf.InverseLerp(squareRect.x, squareRect.xMax, e.mousePosition.x);
                float v = Mathf.InverseLerp(squareRect.yMax, squareRect.y, e.mousePosition.y);
                if (!Mathf.Approximately(s, saturation) || !Mathf.Approximately(v, value))
                {
                    saturation = s;
                    value = v;
                    changed = true;
                }
                e.Use();
            }

            Widgets.DrawBoxSolid(hueRect.ExpandedBy(1f), Color.grey);
            Widgets.DrawTexturePart(hueRect, new Rect(0f, 0f, 1f, 1f), HueChart);

            float markerY = Mathf.Lerp(hueRect.yMax, hueRect.y, hue);
            GUI.color = Color.white;
            Widgets.DrawBox(new Rect(hueRect.x - 3f, markerY - 3f, hueRect.width + 6f, 6f), 2);

            Widgets.DrawBoxSolid(squareRect.ExpandedBy(1f), Color.grey);
            Widgets.DrawBoxSolid(squareRect, Color.white);
            GUI.color = Color.HSVToRGB(hue, 1f, 1f);
            Widgets.DrawTextureFitted(squareRect, ColorChart, 1f);
            GUI.color = Color.white;

            GUI.BeginClip(squareRect);
            Rect marker = new Rect(0f, 0f, MarkerSize, MarkerSize)
            {
                center = new Vector2(squareRect.width * saturation, squareRect.height * (1f - value)).Rounded()
            };
            GUI.color = value >= 0.5f ? DarkMarker : Color.white;
            Widgets.DrawBox(marker, 2);
            GUI.color = Color.white;
            GUI.EndClip();

            return changed;
        }
    }
}