using System.Collections;
using UnityEngine;

// Shared lifecycle and geometric queries only; each species owns its attack.
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
public abstract class NormalMonsterAI : MonoBehaviour, IMonsterAI
{
    [SerializeField] protected Transform body;
    [SerializeField] protected LayerMask wallLayer = 128;
    [SerializeField] protected Material warningMaterial;
    [SerializeField] protected float moveSpeed = 2.4f, attackDistance = 4.5f;
    [SerializeField] protected float warningDuration = .75f, attackInterval = 3.2f, recoveryDuration = .8f;
    [SerializeField] protected int attackDamage = 15;
    [SerializeField] protected Color attackColor = new Color(1f,.35f,.15f);
    [SerializeField] protected float shakeStrength = .035f, shakeDuration = .1f;
    [SerializeField, Min(0), Tooltip("임시 검증 기본값 6. 최종 밸런스 미확정. 최초 인식에만 사용합니다.")]
    protected float aggroRadius = 6f;
    bool hasAggro;
    public bool HasAggro => hasAggro;
    public float AggroRadius => aggroRadius;
    protected Rigidbody2D rb;
    protected Collider2D hull;
    protected PlayerHealth target;
    protected Transform effects;
    protected StoneGolemTelegraph warning;
    protected Vector2 origin, direction, destination;
    protected float nextAttack;
    protected bool busy;
    RoomWalkableLayout layout;
    protected Vector3 bodyRest;
    RigidbodyConstraints2D savedConstraints;
    bool positionLocked;
    protected void LockPosition() { if(positionLocked) return; savedConstraints=rb.constraints; rb.constraints|=RigidbodyConstraints2D.FreezePosition; positionLocked=true; }
    protected void UnlockPosition() { if(!positionLocked || !rb) return; rb.constraints=savedConstraints; positionLocked=false; }
    public bool IsAttacking => busy;
    public PlayerHealth Target => target;
    protected virtual void Awake() { rb=GetComponent<Rigidbody2D>(); hull=GetComponent<Collider2D>(); layout=GetComponentInParent<RoomWalkableLayout>(); if(body) bodyRest=body.localPosition; }
    public void SetTarget(PlayerHealth value) { if(target) target.Died-=TargetDied; CancelAttacks(); target=value; if(target && isActiveAndEnabled) target.Died+=TargetDied; }
    void TargetDied(PlayerHealth _) { CancelAttacks(); }
    void OnDamaged(EnemyHealth _) { hasAggro = true; }
    protected virtual void OnEnable()
    {
        GetComponent<EnemyHealth>().Damaged += OnDamaged;
        if(target) { target.Died-=TargetDied; target.Died+=TargetDied; }
    }
    protected virtual void OnDisable()
    {
        GetComponent<EnemyHealth>().Damaged -= OnDamaged;
        if(target) target.Died-=TargetDied; CancelAttacks();
    }
    public virtual void CancelAttacks()
    {
        StopAllCoroutines(); busy=false; UnlockPosition(); if(rb) rb.linearVelocity=Vector2.zero;
        if(body) body.localPosition=bodyRest;
        if(effects) { effects.gameObject.SetActive(false); Destroy(effects.gameObject); } effects=null; warning=null;
    }
    protected virtual void FixedUpdate()
    {
        if(!target || target.IsDead || GetComponent<EnemyHealth>().IsDead) { rb.linearVelocity=Vector2.zero; return; }
        if (!hasAggro)
        {
            hasAggro = MonsterAggro.CanRecognize(rb.position, target, aggroRadius, wallLayer);
            if (!hasAggro) { rb.linearVelocity = Vector2.zero; return; }
        }
        if(busy) { rb.linearVelocity=Vector2.zero; return; }
        Vector2 delta=(Vector2)target.transform.position-rb.position;
        if(Time.time>=nextAttack && delta.magnitude<=attackDistance && !Physics2D.Linecast(rb.position,target.transform.position,wallLayer))
        {
            origin=rb.position; direction=delta.sqrMagnitude>.0001f?delta.normalized:Vector2.right; destination=target.transform.position;
            busy=true; rb.linearVelocity=Vector2.zero; LockPosition(); nextAttack=Time.time+attackInterval; StartCoroutine(Attack());
        }
        else IdleMove(delta);
    }
    protected virtual void IdleMove(Vector2 delta)
    {
        bool blocked=Physics2D.Linecast(rb.position,target.transform.position,wallLayer);
        Vector2 dir=layout ? layout.Direction(rb.position,target.transform.position,
            Mathf.Max(hull.bounds.extents.x,hull.bounds.extents.y)+.05f) : delta.normalized;
        Move(dir, blocked ? moveSpeed : Mathf.Min(moveSpeed,Mathf.Max(0,delta.magnitude-1.2f)/Time.fixedDeltaTime));
    }
    protected void Move(Vector2 dir,float speed)
    {
        float distance=speed*Time.fixedDeltaTime;
        var filter=new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers=true;
        var hits=new RaycastHit2D[8]; int n=rb.Cast(dir,filter,hits,distance+.02f);
        for(int i=0;i<n;i++) distance=Mathf.Min(distance,Mathf.Max(0,hits[i].distance-.02f));
        rb.linearVelocity=dir*distance/Time.fixedDeltaTime;
    }
    protected Transform Effects()
    {
        if(!effects) { var go=new GameObject(name+" Owned Attacks"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene); effects=go.transform; }
        return effects;
    }
    protected StoneGolemTelegraph Display(Vector2 point,float radius,bool line=false,float length=0)
    { return StoneGolemTelegraph.Create(Effects(),warningMaterial,point,direction,radius,360,length,line,attackColor,0,2); }
    protected IEnumerator Warn(Vector2 point,float radius,bool line=false,float length=0)
    {
        warning=Display(point,radius,line,length); float elapsed=0;
        while(elapsed<warningDuration) { warning.SetProgress(elapsed/Mathf.Max(.001f,warningDuration)); yield return null; elapsed+=Time.deltaTime; }
        warning.SetProgress(1);
    }
    protected void ClearWarning() { if(warning) Destroy(warning.gameObject); warning=null; }
    protected IEnumerator Recover(float seconds) { rb.linearVelocity=Vector2.zero; LockPosition(); yield return new WaitForSeconds(seconds); UnlockPosition(); busy=false; }
    protected bool HitArea(Vector2 point,float radius)
    {
        if(!target || target.IsDead) return false;
        foreach(var c in target.GetComponentsInChildren<Collider2D>())
        {
            if(!c.enabled || c.isTrigger) continue;
            Vector2 closest=c.ClosestPoint(point);
            if(Vector2.Distance(point,closest)<=radius && !Physics2D.Linecast(point,closest,wallLayer)) return target.TryTakeDamage(attackDamage);
        }
        return false;
    }
    protected float WallDistance(Vector2 point,Vector2 dir,float radius,float length)
    { var hit=Physics2D.CircleCast(point,radius,dir,length,wallLayer); return hit?Mathf.Max(0,hit.distance-.02f):length; }
    protected bool SweepPlayer(Vector2 point,Vector2 dir,float radius,float length,out float distance)
    {
        distance=length;
        bool found=false;
        foreach(var hit in Physics2D.CircleCastAll(point,radius,dir,length))
            if(hit.collider && !hit.collider.isTrigger && hit.collider.GetComponentInParent<PlayerHealth>()==target) { distance=Mathf.Min(distance,hit.distance); found=true; }
        return found;
    }
    protected void Impact(Vector2 point,float radius,bool shake=true)
    {
        StartCoroutine(Pulse(point,radius));
        StartCoroutine(Fragments(point));
        if(shake && Camera.main && Camera.main.TryGetComponent<CameraFollow>(out var camera)) camera.ShakeAt(point,shakeStrength,shakeDuration);
    }
    protected IEnumerator Fragments(Vector2 point)
    {
        var source=body?body.GetComponentInChildren<SpriteRenderer>():null;
        if(!source) yield break;
        var pieces=new SpriteRenderer[4]; var velocities=new Vector2[4];
        for(int i=0;i<4;i++)
        {
            pieces[i]=new GameObject("Decorative fragment").AddComponent<SpriteRenderer>();
            pieces[i].transform.SetParent(Effects(),false); pieces[i].transform.position=point;
            pieces[i].sprite=source.sprite; pieces[i].sortingOrder=3;
            pieces[i].transform.localScale=Vector3.one*Random.Range(.07f,.13f);
            velocities[i]=Random.insideUnitCircle*2f;
        }
        float t=0; while(t<.3f)
        {
            for(int i=0;i<4;i++) { pieces[i].transform.position+=(Vector3)(velocities[i]*Time.deltaTime); pieces[i].transform.Rotate(0,0,200*Time.deltaTime); var c=attackColor;c.a=.6f*(1-t/.3f);pieces[i].color=c; }
            yield return null; t+=Time.deltaTime;
        }
        foreach(var piece in pieces) Destroy(piece.gameObject);
    }
    protected IEnumerator BodyGlow(float seconds,bool follow)
    {
        var source=body?body.GetComponentInChildren<SpriteRenderer>():null;
        if(!source) yield break;
        var glow=new GameObject("Attack body cue").AddComponent<SpriteRenderer>();
        glow.transform.SetParent(Effects(),false); glow.sprite=source.sprite; glow.sortingOrder=source.sortingOrder+1;
        glow.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation); glow.transform.localScale=source.transform.lossyScale;
        float t=0; while(t<seconds)
        { if(follow) glow.transform.position=source.transform.position; var c=attackColor;c.a=.35f*(1-t/Mathf.Max(.01f,seconds));glow.color=c;yield return null;t+=Time.deltaTime; }
        Destroy(glow.gameObject);
    }
    IEnumerator Pulse(Vector2 point,float radius)
    {
        var v=Display(point,radius); float t=0;
        while(t<.2f) { v.SetProgress(t/.2f); yield return null; t+=Time.deltaTime; }
        Destroy(v.gameObject);
    }
    protected abstract IEnumerator Attack();
}
