using System.Collections.Generic;
using UnityEngine;

// Local-space floor and rock rectangles are the shared source for geometry and navigation.
// Camera bounds remain on DungeonRoom; internal rocks never constrain the camera.
public sealed class RoomWalkableLayout : MonoBehaviour
{
    public Rect[] floorAreas;
    public Rect[] rockAreas;
    public Rect[] monsterAreas;
    public Vector2[] exitCandidates;
    public int selectedExit;
    public bool chooseExitOnAwake;
    public RoomExit roomExit;
    public GameObject[] unusedExitBarriers;
    const float Cell = .5f;
    readonly Dictionary<int, int[]> fields = new Dictionary<int, int[]>();
    bool[] walkable;
    int width, height, lastGoal = -1;
    Vector2 minimum;
    float lastRadius = -1;

    void Awake()
    {
        if (chooseExitOnAwake) SelectExit(Random.Range(0, exitCandidates.Length));
    }

    public void SelectExit(int index)
    {
        selectedExit = Mathf.Clamp(index, 0, exitCandidates.Length - 1);
        roomExit.transform.localPosition = exitCandidates[selectedExit];
        for (int i = 0; i < unusedExitBarriers.Length; i++)
            unusedExitBarriers[i].SetActive(i != selectedExit);
        walkable=null;fields.Clear();
    }

    IEnumerable<Rect> BlockingAreas()
    {
        foreach(var rock in rockAreas)yield return rock;
        if(chooseExitOnAwake)
            for(int i=0;i<exitCandidates.Length;i++)if(i!=selectedExit)
                yield return new Rect(exitCandidates[i]-new Vector2(1.5f,.75f),new Vector2(3,1.5f));
    }

    public bool ContainsBody(Vector2 world, float radius)
    {
        Vector2 p = transform.InverseTransformPoint(world);
        var room = GetComponent<DungeonRoom>();
        Vector2 lo = transform.InverseTransformPoint(room.WorldMin);
        Vector2 hi = transform.InverseTransformPoint(room.WorldMax);
        if (p.x-radius < lo.x || p.y-radius < lo.y || p.x+radius > hi.x || p.y+radius > hi.y) return false;
        var body = new Rect(p - Vector2.one * radius, Vector2.one * radius * 2);
        foreach (var rock in BlockingAreas()) if (rock.Overlaps(body)) return false;
        // Rock geometry is the exact complement of the floor union.
        foreach (var floor in floorAreas) if (floor.Contains(p)) return true;
        return false;
    }

    public bool SegmentClear(Vector2 a, Vector2 b, float radius)
    {
        if (!ContainsBody(a,radius) || !ContainsBody(b,radius)) return false;
        Vector2 from=transform.InverseTransformPoint(a), to=transform.InverseTransformPoint(b);
        Vector2 delta=to-from;
        foreach(var rock in BlockingAreas())
        {
            float near=0, far=1;
            bool hit=true;
            for(int axis=0;axis<2;axis++)
            {
                float low=(axis==0?rock.xMin:rock.yMin)-radius;
                float high=(axis==0?rock.xMax:rock.yMax)+radius;
                if(Mathf.Abs(delta[axis])<.000001f)
                { if(from[axis]<=low || from[axis]>=high) { hit=false;break; } }
                else
                {
                    float t1=(low-from[axis])/delta[axis], t2=(high-from[axis])/delta[axis];
                    near=Mathf.Max(near,Mathf.Min(t1,t2));far=Mathf.Min(far,Mathf.Max(t1,t2));
                    if(near>=far) { hit=false;break; }
                }
            }
            if(hit)return false;
        }
        return true;
    }

    Vector2 Point(int i) => (Vector2)transform.TransformPoint(minimum + new Vector2(i % width, i / width) * Cell);

    int Nearest(Vector2 p, float connectionRadius = -1f)
    {
        if(connectionRadius<0)connectionRadius=lastRadius;
        Vector2 local = (Vector2)transform.InverseTransformPoint(p) - minimum;
        int x = Mathf.Clamp(Mathf.RoundToInt(local.x / Cell), 0, width - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(local.y / Cell), 0, height - 1);
        int found = -1;
        float best = float.PositiveInfinity;
        for (int dy = -3; dy <= 3; dy++) for (int dx = -3; dx <= 3; dx++)
        {
            int nx=x+dx, ny=y+dy;
            if(nx<0 || ny<0 || nx>=width || ny>=height) continue;
            int i=ny*width+nx;
            float d=(Point(i)-p).sqrMagnitude;
            if(walkable[i] && d<best && SegmentClear(p,Point(i),connectionRadius)) { best=d; found=i; }
        }
        return found;
    }

    void Build(float radius)
    {
        // A conservative common hull covers the largest current monster (iron diameter 1.2).
        radius = Mathf.Max(.65f, Mathf.Ceil((radius - .0001f) * 20) / 20);
        if (walkable != null && Mathf.Approximately(lastRadius,radius)) return;
        lastRadius=radius;
        var room=GetComponent<DungeonRoom>();
        minimum=transform.InverseTransformPoint(room.WorldMin);
        Vector2 maximum=transform.InverseTransformPoint(room.WorldMax);
        width=Mathf.RoundToInt((maximum.x-minimum.x)/Cell)+1;
        height=Mathf.RoundToInt((maximum.y-minimum.y)/Cell)+1;
        walkable=new bool[width*height];
        for(int i=0;i<walkable.Length;i++) walkable[i]=ContainsBody(Point(i),radius);
        fields.Clear(); lastGoal=-1;
    }

    int[] Field(int goal)
    {
        // All monsters share a flow field for the current target cell.
        if(lastGoal!=goal) { fields.Clear(); lastGoal=goal; }
        if(fields.TryGetValue(goal,out var result)) return result;
        result=new int[walkable.Length];
        for(int i=0;i<result.Length;i++) result[i]=-1;
        var queue=new Queue<int>(); queue.Enqueue(goal); result[goal]=0;
        while(queue.Count>0)
        {
            int i=queue.Dequeue(), x=i%width, y=i/width;
            for(int d=0;d<4;d++)
            {
                int nx=x+(d==0?1:d==1?-1:0), ny=y+(d==2?1:d==3?-1:0);
                if(nx<0 || ny<0 || nx>=width || ny>=height) continue;
                int n=ny*width+nx;
                if(!walkable[n] || result[n]>=0) continue;
                result[n]=result[i]+1; queue.Enqueue(n);
            }
        }
        fields[goal]=result; return result;
    }

    public Vector2 Direction(Vector2 from, Vector2 target, float radius)
    {
        Build(radius);
        // Attacks can finish closer to a wall than the conservative navigation margin.
        // Recover along a sweep of the actual hull before following the shared grid.
        if(!ContainsBody(from,lastRadius))
        {
            int recovery=Nearest(from,Mathf.Max(.01f,radius-.05f));
            if(recovery<0)return Vector2.zero;
            Vector2 escape=Point(recovery)-from;
            return escape/Mathf.Max(.15f,escape.magnitude);
        }
        if(SegmentClear(from,target,lastRadius)) return (target-from).normalized;
        // A player hugging a wall is smaller than a monster: project its goal onto
        // a reachable clearance cell without projecting across an intervening rock.
        int goal=Nearest(target,0), start=Nearest(from);
        if(goal<0 || start<0) return Vector2.zero;
        var distances=Field(goal);
        if(distances[start]<0) return Vector2.zero;
        int current=start, next=start;
        // Look ahead along the field, retaining only body-clear straight segments.
        for(int step=0;step<12 && distances[current]>0;step++)
        {
            int best=current, x=current%width, y=current/width;
            for(int d=0;d<4;d++)
            {
                int nx=x+(d==0?1:d==1?-1:0), ny=y+(d==2?1:d==3?-1:0);
                if(nx<0 || ny<0 || nx>=width || ny>=height) continue;
                int n=ny*width+nx;
                if(distances[n]>=0 && distances[n]<distances[best]) best=n;
            }
            if(best==current) break;
            current=best;
            if(SegmentClear(from,Point(current),lastRadius)) next=current; else break;
        }
        Vector2 steering=Point(next)-from;
        return steering/Mathf.Max(.15f,steering.magnitude);
    }

    public bool Reachable(Vector2 from, Vector2 target, float radius)
    {
        Build(radius);
        int a=Nearest(from), b=Nearest(target);
        return a>=0 && b>=0 && Field(b)[a]>=0;
    }
}
