using UnityEngine;
using UnityEngine.UI;

// A single UI mesh: room terrain plus live markers. No camera or world objects.
public sealed class CurrentRoomMapGraphic : MaskableGraphic
{
    DungeonRoom room;
    RoomWalkableLayout layout;
    PlayerHealth player;
    EnemyHealth[] enemies = System.Array.Empty<EnemyHealth>();
    RoomExit exit;
    Vector2 minimum, center;
    float scale;
    public int LivingMarkerCount { get; private set; }
    public bool HasExitMarker => exit != null;
    public Vector2 DisplaySize { get; private set; }
    public void Bind(DungeonRoom value, PlayerHealth target)
    {
        room = value; player = target;
        layout = room ? room.GetComponent<RoomWalkableLayout>() : null;
        enemies = room ? room.GetComponentsInChildren<EnemyHealth>(true) : System.Array.Empty<EnemyHealth>();
        exit = room ? room.GetComponentInChildren<RoomExit>(true) : null;
        LivingMarkerCount = 0; SetVerticesDirty();
    }
    public Vector2 WorldToMap(Vector2 world) => (world - center) * scale;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); LivingMarkerCount = 0;
        if (!room || !layout) return;
        minimum = room.WorldMin;
        var size = room.WorldMax - minimum;
        if (size.x <= 0 || size.y <= 0) return;
        scale = Mathf.Min(rectTransform.rect.width / size.x, rectTransform.rect.height / size.y);
        center = minimum + size * .5f; DisplaySize = size * scale;
        Box(vh, -DisplaySize*.5f, DisplaySize*.5f, new Color(.18f,.19f,.20f,.95f));
        foreach (var rect in layout.floorAreas) Terrain(vh, rect, new Color(.42f,.41f,.36f,.92f));
        foreach (var rect in layout.rockAreas) Terrain(vh, rect, new Color(.18f,.19f,.20f,.95f));
        if (exit && exit.gameObject.activeInHierarchy)
        {
            var p = WorldToMap(exit.transform.position);
            Box(vh, p-Vector2.one*5, p+Vector2.one*5, exit.IsOpen ? new Color(.25f,1,.55f) : new Color(1,.73f,.25f));
        }
        foreach (var enemy in enemies)
        {
            if (!enemy || enemy.IsDead || !enemy.gameObject.activeInHierarchy) continue;
            bool boss = enemy.TryGetComponent<StoneGolemBossAI>(out _);
            Diamond(vh, WorldToMap(enemy.transform.position), boss ? 7 : 4.5f, boss ? new Color(1,.5f,.2f) : new Color(1,.3f,.35f));
            LivingMarkerCount++;
        }
        if (player && !player.IsDead)
        {
            var p = WorldToMap(player.transform.position);
            int i = vh.currentVertCount; var c = new Color(.2f,.95f,1);
            vh.AddVert(p+new Vector2(0,7), c, Vector2.zero);
            vh.AddVert(p+new Vector2(6,-5), c, Vector2.zero);
            vh.AddVert(p+new Vector2(-6,-5), c, Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);
        }
    }
    void Terrain(VertexHelper vh, Rect rect, Color tint)
    {
        Box(vh, WorldToMap(room.transform.TransformPoint(rect.min)), WorldToMap(room.transform.TransformPoint(rect.max)), tint);
    }
    static void Box(VertexHelper vh, Vector2 lo, Vector2 hi, Color tint)
    {
        int i=vh.currentVertCount;
        vh.AddVert(new Vector2(lo.x,lo.y),tint,Vector2.zero); vh.AddVert(new Vector2(lo.x,hi.y),tint,Vector2.zero);
        vh.AddVert(new Vector2(hi.x,hi.y),tint,Vector2.zero); vh.AddVert(new Vector2(hi.x,lo.y),tint,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
    }
    static void Diamond(VertexHelper vh, Vector2 p, float r, Color tint)
    {
        int i=vh.currentVertCount;
        vh.AddVert(p+Vector2.up*r,tint,Vector2.zero); vh.AddVert(p+Vector2.right*r,tint,Vector2.zero);
        vh.AddVert(p+Vector2.down*r,tint,Vector2.zero); vh.AddVert(p+Vector2.left*r,tint,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
    }
}
