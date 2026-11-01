using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace TONE;

// Shared procedural sprites use the game's sprite material without per-frame texture uploads.
internal sealed class MainMenuLiquidGlass
{
    private const float PixelsPerUnit = 256f;
    private static readonly Dictionary<Vector2Int, (Sprite Body, Sprite Rim, Sprite Shadow)> Sprites = new();
    private static readonly Dictionary<(Sprite, Vector2), Sprite> PivotSprites = new();
    private static Sprite reflectionSprite;
    private readonly PassiveButton button;
    private readonly Vector3 originalScale;
    private readonly Surface normal;
    private readonly Surface selected;
    private float hover;
    private float press;

    public bool Hovered { get; set; }

    private MainMenuLiquidGlass(PassiveButton button, Surface normal, Surface selected)
    {
        this.button = button;
        this.normal = normal;
        this.selected = selected;
        originalScale = button.transform.localScale;
    }

    public static MainMenuLiquidGlass Create(PassiveButton button)
    {
        if (button.inactiveSprites == null || button.activeSprites == null) return null;
        var normalRenderer = button.inactiveSprites.GetComponent<SpriteRenderer>();
        var selectedRenderer = button.activeSprites.GetComponent<SpriteRenderer>();
        if (normalRenderer == null || selectedRenderer == null || normalRenderer.sprite == null || selectedRenderer.sprite == null)
            return null;

        var normal = new Surface(normalRenderer);
        return new(button, normal, selectedRenderer == normalRenderer ? normal : new Surface(selectedRenderer));
    }

    public void Press() => press = 1f;

    public void Dispose()
    {
        normal.Dispose();
        if (selected != normal) selected.Dispose();
    }

    public void Update(Camera camera, bool useMouse)
    {
        if (!button.gameObject.activeInHierarchy || !button.enabled)
        {
            Hovered = false;
            hover = press = 0f;
            button.transform.localScale = originalScale;
            normal.Update(0f, null, 1f);
            selected.Update(0f, null, 1f);
            return;
        }

        float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
        hover = Mathf.Lerp(hover, Hovered ? 1f : 0f, blend);
        press = Mathf.MoveTowards(press, 0f, Time.unscaledDeltaTime * 5f);
        float held = useMouse && Hovered && Input.GetMouseButton(0) ? 1f : 0f;
        float scale = 1f + hover * 0.025f - Mathf.Max(press, held) * 0.045f;
        button.transform.localScale = Vector3.Lerp(button.transform.localScale,
            new(originalScale.x * scale, originalScale.y * scale, originalScale.z), blend);

        Vector3? pointer = null;
        if (useMouse && Hovered && camera != null)
        {
            var screenPoint = Input.mousePosition;
            screenPoint.z = camera.WorldToScreenPoint(button.transform.position).z;
            pointer = camera.ScreenToWorldPoint(screenPoint);
        }
        normal.Update(hover, pointer, blend);
        selected.Update(hover, pointer, blend);
    }

    private sealed class Surface
    {
        private readonly SpriteRenderer body;
        private readonly SpriteRenderer rim;
        private readonly ClippedReflection reflection;
        private readonly SpriteRenderer shadow;
        private readonly Vector2 size;
        private readonly Vector3 center;
        private readonly Color tint;

        public Surface(SpriteRenderer renderer)
        {
            body = renderer;
            size = renderer.drawMode == SpriteDrawMode.Simple ? (Vector2)renderer.sprite.bounds.size : renderer.size;
            center = renderer.localBounds.center;
            tint = Color.Lerp(new Color(0.62f, 0.78f, 0.91f), renderer.color, 0.18f);
            var sprites = GetSprites(size);
            // Retain off-center pivots used by the game's left-aligned menu buttons.
            var pivot = renderer.sprite.pivot / renderer.sprite.rect.size;
            if (!PivotSprites.TryGetValue((sprites.Body, pivot), out var bodySprite))
            {
                bodySprite = Sprite.Create(sprites.Body.texture, sprites.Body.rect, pivot,
                    PixelsPerUnit, 0, SpriteMeshType.FullRect, sprites.Body.border);
                bodySprite.hideFlags = HideFlags.HideAndDontSave;
                PivotSprites.Add((sprites.Body, pivot), bodySprite);
            }
            body.sprite = bodySprite;
            body.drawMode = SpriteDrawMode.Sliced;
            body.size = size;

            shadow = AddLayer("TONEGlassShadow", sprites.Shadow, center + new Vector3(0f, -0.025f, 0.025f), size + new Vector2(0.06f, 0.06f));
            rim = AddLayer("TONEGlassRim", sprites.Rim, center + new Vector3(0f, 0f, -0.002f), size);
            var reflectionLayer = AddLayer("TONEGlassReflection", GetReflectionSprite(), center + new Vector3(0f, 0f, -0.004f), size * 0.65f);
            reflection = new(reflectionLayer, size, center, sprites.Body.border.x / PixelsPerUnit);
            Update(0f, null, 1f);
        }

        private SpriteRenderer AddLayer(string name, Sprite sprite, Vector3 position, Vector2 dimensions)
        {
            var obj = new GameObject(name);
            obj.layer = body.gameObject.layer;
            obj.transform.SetParent(body.transform, false);
            obj.transform.localPosition = position;
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = body.sharedMaterial;
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = body.sortingOrder;
            renderer.maskInteraction = body.maskInteraction;
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = dimensions;
            return renderer;
        }

        public void Update(float hover, Vector3? pointer, float blend)
        {
            body.color = new(tint.r, tint.g, tint.b, Mathf.Lerp(0.76f, 0.96f, hover));
            rim.color = new(0.88f, 0.96f, 1f, Mathf.Lerp(0.58f, 0.95f, hover));
            shadow.color = new(0.02f, 0.04f, 0.09f, Mathf.Lerp(0.3f, 0.42f, hover));
            reflection.Renderer.color = new(0.91f, 0.98f, 1f, Mathf.Lerp(0.14f, 0.36f, hover));

            var local = pointer.HasValue ? body.transform.InverseTransformPoint(pointer.Value) : center;
            var target = new Vector2(Mathf.Clamp(local.x - center.x, -size.x * 0.5f, size.x * 0.5f),
                Mathf.Clamp(local.y - center.y, -size.y * 0.5f, size.y * 0.5f));
            reflection.Update(target, blend);
        }

        public void Dispose() => reflection.Dispose();
    }

    // Clip the moving highlight's mesh to the glass, keeping its SpriteRenderer mask behavior.
    // The texture and geometry buffers are reused while the pointer moves all the way to an edge.
    private sealed class ClippedReflection
    {
        private const int CornerSegments = 12;
        private readonly Vector2[] outline = new Vector2[4 * (CornerSegments + 1)];
        private readonly Dictionary<int, (Il2CppStructArray<Vector2> Vertices, Il2CppStructArray<ushort> Triangles)> geometry = new();
        private readonly Sprite sprite;
        private readonly Vector2 dimensions;
        private readonly Vector2 spriteRectSize;
        private readonly Vector3 center;
        private List<Vector2> input = new(64);
        private List<Vector2> output = new(64);
        private Vector2 offset;
        private bool initialized;

        public SpriteRenderer Renderer { get; }

        public ClippedReflection(SpriteRenderer renderer, Vector2 size, Vector3 center, float radius)
        {
            Renderer = renderer;
            this.center = center;
            dimensions = size * 0.65f;
            var source = renderer.sprite;
            spriteRectSize = source.rect.size;
            // Each surface needs its own mesh, while all highlights share one texture.
            sprite = Sprite.Create(source.texture, source.rect, Vector2.one * 0.5f,
                PixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.transform.localScale = new(dimensions.x * PixelsPerUnit / spriteRectSize.x,
                dimensions.y * PixelsPerUnit / spriteRectSize.y, 1f);

            var half = size * 0.5f - Vector2.one * (2f / PixelsPerUnit);
            radius = Mathf.Min(radius, Mathf.Min(half.x, half.y));
            for (int corner = 0; corner < 4; corner++)
            {
                var arcCenter = new Vector2((corner is 0 or 3 ? 1f : -1f) * (half.x - radius),
                    (corner < 2 ? 1f : -1f) * (half.y - radius));
                for (int segment = 0; segment <= CornerSegments; segment++)
                {
                    float angle = (corner + (float)segment / CornerSegments) * Mathf.PI * 0.5f;
                    outline[corner * (CornerSegments + 1) + segment] = arcCenter
                        + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }
        }

        public void Update(Vector2 target, float blend)
        {
            var next = Vector2.Lerp(offset, target, blend);
            if (initialized && (next - offset).sqrMagnitude < 0.00000001f) return;
            initialized = true;
            offset = next;
            Renderer.transform.localPosition = center + new Vector3(offset.x, offset.y, -0.004f);

            input.Clear();
            input.AddRange(outline);
            Clip(offset.x - dimensions.x * 0.5f, true, true);
            Clip(offset.x + dimensions.x * 0.5f, true, false);
            Clip(offset.y - dimensions.y * 0.5f, false, true);
            Clip(offset.y + dimensions.y * 0.5f, false, false);

            int count = input.Count;
            Renderer.enabled = count >= 3;
            if (count < 3) return;
            if (!geometry.TryGetValue(count, out var buffers))
            {
                buffers = (new Il2CppStructArray<Vector2>(count), new Il2CppStructArray<ushort>((count - 2) * 3));
                for (int i = 0; i < count - 2; i++)
                {
                    buffers.Triangles[i * 3] = 0;
                    buffers.Triangles[i * 3 + 1] = (ushort)(i + 2);
                    buffers.Triangles[i * 3 + 2] = (ushort)(i + 1);
                }
                geometry.Add(count, buffers);
            }
            for (int i = 0; i < count; i++)
            {
                var point = (input[i] - offset) / dimensions;
                // OverrideGeometry takes pixel coordinates in Sprite.rect, and applies the pivot itself.
                buffers.Vertices[i] = new Vector2((Mathf.Clamp(point.x, -0.5f, 0.5f) + 0.5f) * spriteRectSize.x,
                    (Mathf.Clamp(point.y, -0.5f, 0.5f) + 0.5f) * spriteRectSize.y);
            }
            sprite.OverrideGeometry(buffers.Vertices, buffers.Triangles);
        }

        private void Clip(float boundary, bool horizontal, bool keepGreater)
        {
            if (input.Count == 0) return;
            output.Clear();
            var previous = input[^1];
            float previousValue = horizontal ? previous.x : previous.y;
            bool previousInside = keepGreater ? previousValue >= boundary : previousValue <= boundary;
            foreach (var point in input)
            {
                float value = horizontal ? point.x : point.y;
                bool inside = keepGreater ? value >= boundary : value <= boundary;
                if (inside != previousInside)
                    output.Add(Vector2.LerpUnclamped(previous, point, (boundary - previousValue) / (value - previousValue)));
                if (inside) output.Add(point);
                previous = point;
                previousValue = value;
                previousInside = inside;
            }
            (input, output) = (output, input);
        }

        public void Dispose()
        {
            if (sprite != null) Object.Destroy(sprite);
        }
    }

    private static (Sprite Body, Sprite Rim, Sprite Shadow) GetSprites(Vector2 size)
    {
        var key = new Vector2Int(Mathf.Clamp(Mathf.RoundToInt(size.x * PixelsPerUnit), 32, 1024),
            Mathf.Clamp(Mathf.RoundToInt(size.y * PixelsPerUnit), 32, 256));
        if (Sprites.TryGetValue(key, out var sprites)) return sprites;

        int width = key.x;
        int height = key.y;
        float radius = Mathf.Min(width, height) * 0.42f;
        var body = new Color32[width * height];
        var rim = new Color32[body.Length];
        var shadow = new Color32[body.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f - width * 0.5f;
                float py = y + 0.5f - height * 0.5f;
                float qx = Mathf.Abs(px) - (width * 0.5f - radius - 2f);
                float qy = Mathf.Abs(py) - (height * 0.5f - radius - 2f);
                float distance = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude
                    + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float coverage = Mathf.Clamp01(0.5f - distance);
                float vertical = (float)y / (height - 1);
                float edge = Mathf.Exp(-Mathf.Abs(distance + 1.4f) / 1.5f);
                var color = Color.Lerp(new Color(0.14f, 0.22f, 0.34f), new Color(0.78f, 0.87f, 0.96f), vertical);
                int index = y * width + x;
                color.a = coverage * (0.28f + vertical * 0.08f + edge * 0.16f);
                body[index] = color;
                rim[index] = new Color(1f, 1f, 1f, coverage * edge * (0.28f + vertical * 0.62f));
                shadow[index] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance / 5f) * 0.75f);
            }
        }

        sprites = (MakeSprite("GlassBody", width, height, body, radius),
            MakeSprite("GlassRim", width, height, rim, radius),
            MakeSprite("GlassShadow", width, height, shadow, radius));
        Sprites.Add(key, sprites);
        return sprites;
    }

    private static Sprite GetReflectionSprite()
    {
        if (reflectionSprite != null) return reflectionSprite;
        const int size = 64;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - u * u - v * v), 3f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        return reflectionSprite = MakeSprite("GlassReflection", size, size, pixels, 0f);
    }

    private static Sprite MakeSprite(string name, int width, int height, Color32[] pixels, float border)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), Vector2.one * 0.5f,
            PixelsPerUnit, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
