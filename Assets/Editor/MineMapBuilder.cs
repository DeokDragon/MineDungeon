using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Invoke through Unity MCP. Existing room prefabs and their GUIDs are never overwritten.
public static class MineMapBuilder
{
    public const string Folder = "Assets/Prefabs/Rooms/ReferenceMaps";
    static Rect R(float x, float y, float w, float h) => new Rect(x,y,w,h);
    static Transform Group(Transform parent,string name)
    {
        var t=new GameObject(name).transform; t.SetParent(parent,false); return t;
    }
    static void Set(UnityEngine.Object target,string property,UnityEngine.Object value)
    {
        var so=new SerializedObject(target); so.FindProperty(property).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    static SpriteRenderer Tile(Transform parent,string name,Rect rect,Sprite sprite,Color color,int order,bool solid)
    {
        var go=new GameObject(name); go.transform.SetParent(parent,false);
        go.transform.localPosition=rect.center;
        go.transform.localScale=new Vector3(rect.width,rect.height,1);
        var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.color=color; sr.sortingOrder=order;
        if(solid) { go.layer=LayerMask.NameToLayer("Wall"); go.AddComponent<BoxCollider2D>().size=Vector2.one; }
        return sr;
    }
    static void CollapsedWallDecoration(Transform geometry, Sprite sprite)
    {
        var rubble=Group(geometry,"CollapsedWall_VisualOnly");
        var stones=new[]{R(-13,15,3,2),R(-9,16,2,3),R(-12,10,3,3),R(-8,8,2,2),R(-13,5,3,2),R(-9,3,3,2),R(-7,0,3,1.5f),R(-5,-3,2,1)};
        for(int i=0;i<stones.Length;i++)
            Tile(rubble,"RubbleStone",stones[i],sprite,new Color(.27f,.23f,.18f),-3,false).transform.localRotation=Quaternion.Euler(0,0,i%2==0?12:-16);
        var timbers=new[]{R(-12,13,5,.3f),R(-12,7,5,.3f),R(-10,4,4,.3f),R(-6,-1,3,.3f)};
        for(int i=0;i<timbers.Length;i++)
            Tile(rubble,"BrokenTimber",timbers[i],sprite,new Color(.38f,.29f,.18f),-2,false).transform.localRotation=Quaternion.Euler(0,0,i%2==0?28:-32);
    }
    public static string Build(string id)
    {
        if(Application.isPlaying) throw new InvalidOperationException("Build in edit mode.");
        string path=Folder+"/Map_"+id+".prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)) throw new InvalidOperationException("Candidate already exists: "+path);
        if(!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs/Rooms","ReferenceMaps");
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Rooms/Room_Normal_01.prefab");
        var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
        root.name="Map_"+id;
        try
        {
            var room=root.GetComponent<DungeonRoom>();
            var controller=root.GetComponentInChildren<RoomController>();
            var exit=root.GetComponentInChildren<RoomExit>();
            var entrance=room.Entrance;
            var sprite=root.transform.Find("Map/Floor").GetComponent<SpriteRenderer>().sprite;
            // Only remove geometry in the newly created copy, not in the source asset or scene.
            UnityEngine.Object.DestroyImmediate(root.transform.Find("Map").gameObject);
            var layout=root.AddComponent<RoomWalkableLayout>();
            var floors=new List<Rect>();
            var obstacles=new List<Rect>();
            Vector2 entry=new Vector2(0,-17), door=new Vector2(0,18);
            Vector2[] spawn;
            Rect[] zones;
            switch(id)
            {
                case "06":
                    floors.AddRange(new[]{R(-6,-20,12,10),R(-3,-12,6,25),R(-14,-5,28,10),R(-5,11,10,9)});
                    spawn=new[]{new Vector2(-11,-2),new Vector2(-8,2),new Vector2(0,1),new Vector2(8,-2),new Vector2(11,2),new Vector2(2,15)};
                    zones=new[]{R(-12,-3,6,6),R(-2,-3,4,6),R(6,-3,6,6),R(-3,13,6,4)};
                    break;
                case "04":
                    floors.Add(R(-14,-20,28,40));entry=new Vector2(-9,-17);door=new Vector2(12,16);
                    // Revised reference: collapsed upper-left, split central wall and a 6-unit lower crossing.
                    obstacles.AddRange(new[]{R(-2,-20,4,9),R(-2,-5,4,13),R(7,10,3,3),R(7,-1,3,3),R(-10,-10,2.5f,2.5f),R(7,-11,3,3),R(-14,10,9,10),R(-14,6,10,4),R(-14,2,11,4),R(-8,-2,6,4),R(-6,-4,4,2)});
                    spawn=new[]{new Vector2(2.5f,12),new Vector2(4.5f,14.5f),new Vector2(11.5f,10),new Vector2(11.5f,7),new Vector2(11.5f,-2.5f),new Vector2(11.5f,-5.5f)};
                    zones=new[]{R(1,10.5f,5,6),R(10.5f,5.5f,2.5f,6),R(10,-7,3,6)};
                    break;
                case "05":
                    floors.Add(R(-14,-20,28,40));obstacles.Add(R(-7,-8,14,16));
                    spawn=new[]{new Vector2(-3,15),new Vector2(3,15),new Vector2(-11,-1),new Vector2(-11,3),new Vector2(11,-1),new Vector2(11,3)};
                    zones=new[]{R(-5,13,10,4),R(-13,-3,5,8),R(8,-3,5,8)};
                    break;
                case "01":
                    floors.Add(R(-14,-20,28,40));entry=new Vector2(-10,-17);door=new Vector2(10,18);
                    obstacles.AddRange(new[]{R(-14,-8,21,3),R(-7,5,21,3)});
                    spawn=new[]{new Vector2(9,-16),new Vector2(12,-12),new Vector2(-10,-1),new Vector2(-10,2),new Vector2(-1,15),new Vector2(5,15)};
                    zones=new[]{R(8,-18,5,8),R(-12,-3,4,7),R(-4,12,11,5)};
                    break;
                case "02":
                    floors.Add(R(-14,-20,28,40));door=new Vector2(-8,18);
                    obstacles.AddRange(new[]{R(-1.5f,9,3,11),R(-1.5f,0,3,5),R(-2,-8,4,3)});
                    spawn=new[]{new Vector2(-8,-13),new Vector2(8,-13),new Vector2(-8,1),new Vector2(-8,4),new Vector2(8,1),new Vector2(8,4)};
                    zones=new[]{R(-11,-16,6,6),R(5,-16,6,6),R(-11,-1,6,7),R(5,-1,6,7)};
                    break;
                case "03":
                    floors.Add(R(-14,-20,28,40));entry=new Vector2(11,-17);
                    obstacles.AddRange(new[]{R(-14,-1,11,2),R(3,-1,11,2),R(-8,-10,3,3),R(5,-10,3,3),R(-8,4,3,3),R(5,4,3,3),R(-8,12,3,3),R(5,12,3,3)});
                    spawn=new[]{new Vector2(-10,-16),new Vector2(0,-13),new Vector2(-11,-4),new Vector2(0,9),new Vector2(-3,16),new Vector2(3,16)};
                    zones=new[]{R(-12,-18,4,4),R(-2,-15,4,4),R(-13,-6,4,4),R(-4,8,8,10)};
                    break;
                case "07":
                    floors.Add(R(-14,-20,28,40));entry=new Vector2(-10,-17);obstacles.Add(R(-6,-8,12,16));
                    spawn=new[]{new Vector2(11,-14),new Vector2(-11,1),new Vector2(-9,15),new Vector2(0,15),new Vector2(9,15),new Vector2(11,-11)};
                    zones=new[]{R(9,-16,4,7),R(-13,-2,4,6),R(-11,13,4,4),R(-2,13,4,4),R(7,13,4,4)};
                    break;
                default: throw new ArgumentException("Not yet approved for expansion: "+id);
            }
            layout.floorAreas=floors.ToArray(); layout.monsterAreas=zones;
            var geometry=Group(root.transform,"Map");
            var floorRoot=Group(geometry,"WalkableFloor");
            var rocks=Group(geometry,"BlockingRock");
            var decoration=Group(geometry,"PassableRails");
            var markers=Group(root.transform,"MonsterAreas");
            foreach(var floor in floors) Tile(floorRoot,"Floor",floor,sprite,new Color(.24f,.21f,.17f),-10,false);
            var xs=new SortedSet<float>{-16,16}; var ys=new SortedSet<float>{-22,22};
            foreach(var r in floors) { xs.Add(r.xMin);xs.Add(r.xMax);ys.Add(r.yMin);ys.Add(r.yMax); }
            foreach(var r in obstacles) { xs.Add(r.xMin);xs.Add(r.xMax);ys.Add(r.yMin);ys.Add(r.yMax); }
            var x=new List<float>(xs);var y=new List<float>(ys); var blocked=new List<Rect>();
            for(int j=0;j<y.Count-1;j++) for(int i=0;i<x.Count-1;i++)
            {
                Rect cell=R(x[i],y[j],x[i+1]-x[i],y[j+1]-y[j]);
                bool floor=false;foreach(var r in floors) if(r.Contains(cell.center))floor=true;
                foreach(var r in obstacles) if(r.Contains(cell.center))floor=false;
                if(floor)continue;
                blocked.Add(cell);
                Tile(rocks,"Rock",cell,sprite,new Color(.105f,.09f,.08f),-8,true);
                // Inset visual stone face; collider remains an exact, gap-free rectangle.
                if(cell.width>.2f && cell.height>.2f)
                    Tile(rocks,"RockFace",R(cell.x+.08f,cell.y+.08f,cell.width-.16f,cell.height-.16f),sprite,new Color(.16f,.14f,.12f),-7,false);
            }
            layout.rockAreas=blocked.ToArray();
            // Rails are floor-only decoration; omit any segment that crosses a rock.
            var tracks=new List<Vector2[]>();
            if(id=="06")tracks.Add(new[]{new Vector2(0,-19),new Vector2(0,18)});
            if(id=="04")tracks.Add(new[]{new Vector2(-9,-18),new Vector2(-5,-13),new Vector2(-5,10),new Vector2(-4,15),new Vector2(12,15)});
            if(id=="05")tracks.Add(new[]{new Vector2(-10,-14),new Vector2(-10,11),new Vector2(10,11),new Vector2(10,-14),new Vector2(-10,-14)});
            if(id=="01")tracks.Add(new[]{new Vector2(-10,-18),new Vector2(-10,-12),new Vector2(10,-12),new Vector2(10,0),new Vector2(-10,0),new Vector2(-10,15),new Vector2(10,15),new Vector2(10,18)});
            if(id=="02")foreach(float side in new[]{-8f,8f})tracks.Add(new[]{new Vector2(0,-19),new Vector2(0,-9),new Vector2(side,-4),new Vector2(side,18)});
            foreach(var track in tracks)for(int segment=1;segment<track.Length;segment++)
            {
                Vector2 delta=track[segment]-track[segment-1],direction=delta.normalized;
                Vector2 normal=new Vector2(-direction.y,direction.x);
                float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg-90;
                int count=Mathf.CeilToInt(delta.magnitude/.8f);float step=delta.magnitude/count;
                for(int k=0;k<count;k++)
                {
                    Vector2 p=track[segment-1]+direction*(k+.5f)*step;
                    bool free=true;foreach(var r in blocked)if(r.Overlaps(R(p.x-.7f,p.y-.7f,1.4f,1.4f)))free=false;
                    if(!free)continue;
                    Tile(decoration,"Sleeper",R(p.x-.65f,p.y-.08f,1.3f,.16f),sprite,new Color(.35f,.27f,.18f),-5,false).transform.localRotation=Quaternion.Euler(0,0,angle);
                    foreach(float side in new[]{-.42f,.42f})
                    { Vector2 q=p+normal*side;Tile(decoration,"Rail",R(q.x-.035f,q.y-step*.5f,.07f,step),sprite,new Color(.45f,.43f,.38f),-4,false).transform.localRotation=Quaternion.Euler(0,0,angle); }
                }
            }
            if(id=="04") CollapsedWallDecoration(geometry,sprite);
            if(id=="05")
            {
                // Crane foundation is the blocked central rectangle; arm/hook are visual only.
                var crane=Group(geometry,"CraneDecoration_NoCollision");
                Tile(crane,"Base",R(-2,-2,4,4),sprite,new Color(.3f,.28f,.22f),-2,false);
                var arm=Tile(crane,"Arm",R(-.25f,0,.5f,7),sprite,new Color(.42f,.38f,.26f),-1,false);arm.transform.localRotation=Quaternion.Euler(0,0,-35);
                Tile(crane,"Hook",R(2.7f,3.6f,.3f,.8f),sprite,new Color(.55f,.5f,.35f),-1,false);
            }
            for(int i=0;i<zones.Length;i++)
            {
                var marker=Group(markers,"Region_"+(i+1));marker.localPosition=zones[i].center;
                // Region rectangles are data, never one enemy per marker.
            }
            entrance.localPosition=entry; exit.transform.localPosition=door;exit.transform.localScale=new Vector3(2,1,1);
            layout.roomExit=exit;layout.exitCandidates=new[]{door};layout.unusedExitBarriers=Array.Empty<GameObject>();
            if(id=="02")
            {
                layout.chooseExitOnAwake=true;layout.exitCandidates=new[]{new Vector2(-8,18),new Vector2(8,18)};
                layout.unusedExitBarriers=new GameObject[2];
                for(int i=0;i<2;i++)layout.unusedExitBarriers[i]=Tile(rocks,"UnusedExit_"+i,R(layout.exitCandidates[i].x-1.5f,17.25f,3,1.5f),sprite,new Color(.16f,.14f,.12f),-3,true).gameObject;
                layout.SelectExit(0);
            }
            var so=new SerializedObject(room);
            so.FindProperty("localMin").vector2Value=new Vector2(-16,-22);
            so.FindProperty("localMax").vector2Value=new Vector2(16,22);so.ApplyModifiedPropertiesWithoutUndo();
            var enemies=root.GetComponentsInChildren<EnemyHealth>(true);
            if(enemies.Length!=6)throw new InvalidOperationException("Source enemy budget changed; review before rebuilding.");
            for(int i=0;i<enemies.Length;i++)enemies[i].transform.position=spawn[i];
            foreach(var rb in root.GetComponentsInChildren<Rigidbody2D>())rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            Physics2D.SyncTransforms();
            foreach(var e in enemies)
                if(!layout.ContainsBody(e.transform.position,.65f)||!layout.Reachable(entrance.position,e.transform.position,.65f))
                    throw new InvalidOperationException("Unsafe enemy position: "+e.name);
            if(!layout.Reachable(entry,door-Vector2.up*1.5f,.65f))throw new InvalidOperationException("Exit is unreachable");
            PrefabUtility.SaveAsPrefabAsset(root,path);
            return path+"; total enemies=6; rectangle=32x44; verified body clearance=1.3";
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
