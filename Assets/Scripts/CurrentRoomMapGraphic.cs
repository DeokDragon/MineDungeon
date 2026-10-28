using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Same live room data and coordinates, with a padded texture atlas.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CurrentRoomMapGraphic : MaskableGraphic
{
    DungeonRoom room; RoomWalkableLayout layout; PlayerHealth player; PlayerAttack attack;
    EnemyHealth[] enemies = System.Array.Empty<EnemyHealth>(); RoomExit exit;
    Vector2 minimum, center; float scale; Texture2D atlas;
    readonly List<UIVertex> terrain = new List<UIVertex>(); bool terrainDirty = true;
    public override Texture mainTexture => atlas ? atlas : base.mainTexture;
    public int LivingMarkerCount { get; private set; }
    public bool HasExitMarker => exit != null;
    public Vector2 DisplaySize { get; private set; }
    protected override void Awake() { base.Awake(); atlas = Resources.Load<Texture2D>("TabMapUI_v1/RuntimeAtlas"); }
    public void Bind(DungeonRoom value, PlayerHealth target)
    {
        room=value; player=target; attack=target?target.GetComponent<PlayerAttack>():null;
        layout=room?room.GetComponent<RoomWalkableLayout>():null;
        enemies=room?room.GetComponentsInChildren<EnemyHealth>(true):System.Array.Empty<EnemyHealth>();
        exit=room?room.GetComponentInChildren<RoomExit>(true):null;
        LivingMarkerCount=0;terrainDirty=true;SetVerticesDirty();
    }
    protected override void OnRectTransformDimensionsChange() { terrainDirty=true;base.OnRectTransformDimensionsChange(); }
    public Vector2 WorldToMap(Vector2 world) => (world-center)*scale;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();LivingMarkerCount=0;if(!room||!layout)return;
        minimum=room.WorldMin;var size=room.WorldMax-minimum;if(size.x<=0||size.y<=0)return;
        scale=Mathf.Min(rectTransform.rect.width/size.x,rectTransform.rect.height/size.y);
        center=minimum+size*.5f;DisplaySize=size*scale;
        if(terrainDirty)BuildTerrain();vh.AddUIVertexTriangleStream(terrain);
        if(exit&&exit.gameObject.activeInHierarchy)Icon(vh,WorldToMap(exit.transform.position),24,exit.IsOpen?11:10,0);
        foreach(var enemy in enemies)
        {
            if(!enemy||enemy.IsDead||!enemy.gameObject.activeInHierarchy)continue;
            bool boss=enemy.TryGetComponent<StoneGolemBossAI>(out _);
            Icon(vh,WorldToMap(enemy.transform.position),boss?32:24,boss?9:8,0);LivingMarkerCount++;
        }
        if(player&&!player.IsDead)Icon(vh,WorldToMap(player.transform.position),24,7,attack?attack.VisualAimAngle-90:0);
    }
    Rect MapRect(Rect r)
    {
        var lo=WorldToMap(room.transform.TransformPoint(r.min));var hi=WorldToMap(room.transform.TransformPoint(r.max));
        return Rect.MinMaxRect(lo.x,lo.y,hi.x,hi.y);
    }
    void BuildTerrain()
    {
        terrain.Clear();terrainDirty=false;
        var floors=System.Array.ConvertAll(layout.floorAreas,MapRect);var rocks=System.Array.ConvertAll(layout.rockAreas,MapRect);
        var xs=new SortedSet<float>{-DisplaySize.x/2,DisplaySize.x/2};var ys=new SortedSet<float>{-DisplaySize.y/2,DisplaySize.y/2};
        foreach(var r in floors){xs.Add(r.xMin);xs.Add(r.xMax);ys.Add(r.yMin);ys.Add(r.yMax);}
        foreach(var r in rocks){xs.Add(r.xMin);xs.Add(r.xMax);ys.Add(r.yMin);ys.Add(r.yMax);}
        var x=new float[xs.Count];xs.CopyTo(x);var y=new float[ys.Count];ys.CopyTo(y);
        var walk=new bool[x.Length-1,y.Length-1];
        for(int i=0;i<x.Length-1;i++)for(int j=0;j<y.Length-1;j++)
        {
            var p=new Vector2((x[i]+x[i+1])/2,(y[j]+y[j+1])/2);
            walk[i,j]=System.Array.Exists(floors,r=>r.Contains(p))&&!System.Array.Exists(rocks,r=>r.Contains(p));
        }
        using(var mesh=new VertexHelper())
        {
            for(int i=0;i<x.Length-1;i++)for(int j=0;j<y.Length-1;j++)
            {
                var r=Rect.MinMaxRect(x[i],y[j],x[i+1],y[j+1]);bool rock=System.Array.Exists(rocks,a=>a.Contains(r.center));
                Tiles(mesh,r,walk[i,j]?1:rock?2:0);if(walk[i,j])continue;
                // Boundary art is clipped inside wall cells, never over walkable floor.
                float w=Mathf.Min(6,r.width),h=Mathf.Min(6,r.height);
                if(j+1<y.Length-1&&walk[i,j+1])Strip(mesh,new Rect(r.xMin,r.yMax-h,r.width,h),3,true);
                if(i+1<x.Length-1&&walk[i+1,j])Strip(mesh,new Rect(r.xMax-w,r.yMin,w,r.height),4,false);
                if(j>0&&walk[i,j-1])Strip(mesh,new Rect(r.xMin,r.yMin,r.width,h),5,true);
                if(i>0&&walk[i-1,j])Strip(mesh,new Rect(r.xMin,r.yMin,w,r.height),6,false);
            }
            mesh.GetUIVertexStream(terrain);
        }
    }
    // 4x3 cells: 32px artwork plus 4px padding on each side.
    static Rect UV(int s)=>new Rect((s%4*40+4)/160f,(s/4*40+4)/120f,32/160f,32/120f);
    static void Tiles(VertexHelper vh,Rect r,int s)
    {
        for(float y=r.yMin;y<r.yMax-.001f;)
        {
            float v=Mathf.Repeat(y,32),h=Mathf.Min(32-v,r.yMax-y);
            for(float x=r.xMin;x<r.xMax-.001f;)
            {
                float u=Mathf.Repeat(x,32),w=Mathf.Min(32-u,r.xMax-x);var uv=UV(s);
                Quad(vh,new Rect(x,y,w,h),new Rect(uv.x+u/160,uv.y+v/120,w/160,h/120));x+=w;
            }
            y+=h;
        }
    }
    static void Strip(VertexHelper vh,Rect r,int s,bool horizontal)
    {
        float from=horizontal?r.xMin:r.yMin,to=horizontal?r.xMax:r.yMax;
        for(float p=from;p<to-.001f;)
        {
            float t=Mathf.Repeat(p,32),n=Mathf.Min(32-t,to-p);var uv=UV(s);
            if(horizontal)Quad(vh,new Rect(p,r.yMin,n,r.height),new Rect(uv.x+t/160,uv.y,n/160,uv.height));
            else Quad(vh,new Rect(r.xMin,p,r.width,n),new Rect(uv.x,uv.y+t/120,uv.width,n/120));
            p+=n;
        }
    }
    static void Icon(VertexHelper vh,Vector2 p,float size,int s,float angle)
    {
        var uv=UV(s);int i=vh.currentVertCount;var rot=Quaternion.Euler(0,0,angle);
        var corners=new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,1),new Vector2(1,-1)};
        var uvs=new[]{uv.min,new Vector2(uv.xMin,uv.yMax),uv.max,new Vector2(uv.xMax,uv.yMin)};
        for(int k=0;k<4;k++)vh.AddVert(p+(Vector2)(rot*(Vector3)(corners[k]*size*.5f)),Color.white,uvs[k]);
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
    static void Quad(VertexHelper vh,Rect r,Rect uv)
    {
        int i=vh.currentVertCount;
        vh.AddVert(new Vector2(r.xMin,r.yMin),Color.white,uv.min);
        vh.AddVert(new Vector2(r.xMin,r.yMax),Color.white,new Vector2(uv.xMin,uv.yMax));
        vh.AddVert(new Vector2(r.xMax,r.yMax),Color.white,uv.max);
        vh.AddVert(new Vector2(r.xMax,r.yMin),Color.white,new Vector2(uv.xMax,uv.yMin));
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
}
