using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StoneMonsterAI : MonoBehaviour, IMonsterAI
{
    private enum State { Chase, Warning, Jump, Recovery }

    [Header("연결")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform body;
    [SerializeField] private Transform attackWarning;
    [SerializeField] private LayerMask wallLayer;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float stopDistance = 2.6f;

    [Header("공격")]
    [SerializeField] private float warningDuration = 0.65f;
    [SerializeField, Min(0.01f)] private float jumpDuration = 0.35f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float attackRadius = 0.9f;
    [SerializeField] private int attackDamage = 12;
    [SerializeField] private float recoveryDuration = 1f;
    [SerializeField] private float attackInterval = 2.8f;

    [SerializeField] private float maxJumpDistance=3.2f;
    [SerializeField] private Material warningMaterial;
    [SerializeField] private float shakeStrength=.025f, shakeDuration=.1f;
    private StoneGolemTelegraph telegraph;
    private GameObject effectsRoot;
    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private Collider2D playerCollider;
    private PlayerHealth playerHealth;

    private State state;
    private float timer;
    private float nextAttackTime;
    private Vector2 landingPosition;
    private Vector2 jumpStartPosition;
    private Vector3 bodyStartPosition;
    private bool ignoringPlayer;
    private readonly MonsterCollisionPass passage = new MonsterCollisionPass();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
        if (body != null) bodyStartPosition = body.localPosition;
    }

    private void Start()
    {
        if (player != null && playerHealth == null)
        {
            SetTarget(player.GetComponent<PlayerHealth>());
        }

        if (body != null)
            bodyStartPosition = body.localPosition;

        if (attackWarning != null)
        {
            attackWarning.localScale = new Vector3(
                attackRadius * 2f, attackRadius * 2f, 1f
            );
            attackWarning.gameObject.SetActive(false);
        }

    }

    private void FixedUpdate()
    {
        if (player == null || body == null ||
            playerHealth == null || playerCollider == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;

            ResetVisuals();
            SetIgnorePlayer(false);

            state = State.Chase;
            timer = 0f;

            // 부활 직후 예고 없이 공격하지 않도록 대기
            nextAttackTime = Time.time + 1f;
            return;
        }

        switch (state)
        {
            case State.Chase:
                Chase();
                break;

            case State.Warning:
                rb.linearVelocity = Vector2.zero;
                timer += Time.fixedDeltaTime;
                if(telegraph) telegraph.SetProgress(timer/(warningDuration+jumpDuration));

                if (timer >= warningDuration)
                {
                    state = State.Jump;
                    timer = 0f;
                    jumpStartPosition = rb.position;
                    SetIgnorePlayer(true);
                    passage.Begin(this,playerHealth);
                }
                break;

            case State.Jump:
                Jump();
                break;

            case State.Recovery:
                rb.linearVelocity = Vector2.zero;
                timer += Time.fixedDeltaTime;

                if (timer >= recoveryDuration)
                    state = State.Chase;
                break;
        }
    }

    private void LateUpdate()
    {
        // 親が動いても、予告位置は固定する
        if (attackWarning != null &&
            attackWarning.gameObject.activeSelf)
        {
            attackWarning.position = new Vector3(
                landingPosition.x, landingPosition.y, 0f
            );
        }
    }

    private void Chase()
    {
        Vector2 direction = (Vector2)player.position - rb.position;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            float speed = Mathf.Min(
                moveSpeed,
                (distance - stopDistance) / Time.fixedDeltaTime
            );

            rb.linearVelocity = direction.normalized * speed;
            return;
        }

        rb.linearVelocity = Vector2.zero;

        if (Time.time < nextAttackTime)
            return;

        // 벽으로 가려진 상대에게는 점프를 시작하지 않음
        if (Physics2D.Linecast(
            rb.position, player.position, wallLayer).collider != null)
        {
            return;
        }

        // 예고 시작 시 플레이어 위치를 저장
        Vector2 delta=(Vector2)player.position-rb.position;
        float landingDistance=Mathf.Min(maxJumpDistance,delta.magnitude);
        var filter=new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers=true;
        var hits=new RaycastHit2D[16]; int count=rb.Cast(delta.normalized,filter,hits,landingDistance+.02f);
        for(int i=0;i<count;i++) landingDistance=Mathf.Min(landingDistance,Mathf.Max(0,hits[i].distance-.02f));
        if(landingDistance<.05f) return;
        landingPosition=rb.position+delta.normalized*landingDistance;
        jumpStartPosition=rb.position;
        effectsRoot=new GameObject(name+" Landing Warning");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effectsRoot,gameObject.scene);
        telegraph=StoneGolemTelegraph.Create(effectsRoot.transform,warningMaterial,landingPosition,Vector2.right,attackRadius,360,0,false,new Color(1,.4f,.15f),0,2);
        nextAttackTime = Time.time + attackInterval;
        timer = 0f;
        state = State.Warning;


    }

    private void Jump()
    {
        // 마지막 이동의 물리 처리가 끝난 다음 착지 판정
        if (timer >= jumpDuration)
        {
            Land();
            return;
        }

        timer = Mathf.Min(
            timer + Time.fixedDeltaTime, jumpDuration
        );

        float progress = timer / jumpDuration;
        if(telegraph) telegraph.SetProgress((warningDuration+timer)/(warningDuration+jumpDuration));

        Vector2 nextPosition = Vector2.Lerp(
            jumpStartPosition, landingPosition, progress
        );

        var filter=new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers=true;
        var hits=new RaycastHit2D[8]; Vector2 movement=nextPosition-rb.position;
        int count=rb.Cast(movement.normalized,filter,hits,movement.magnitude);
        if(count>0) { CancelAttacks(); return; }
        rb.MovePosition(nextPosition);

        // 몸체 이미지만 위로 올렸다가 내림
        float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
        body.localPosition =
            bodyStartPosition + Vector3.up * height;
    }

    private void Land()
    {
        rb.linearVelocity = Vector2.zero;
        ResetVisuals();

        Vector2 center = rb.position;
        Vector2 closestPoint = playerCollider.ClosestPoint(center);

        bool inRange =
            Vector2.Distance(center, closestPoint) <= attackRadius;

        bool blocked = Physics2D.Linecast(
            center, closestPoint, wallLayer
        ).collider != null;

        // 착지 한 번당 피해 판정 한 번
        if(Camera.main && Camera.main.TryGetComponent<CameraFollow>(out var camera)) camera.ShakeAt(center,shakeStrength,shakeDuration);
        if (inRange && !blocked) playerHealth.TakeDamage(attackDamage);
        if(playerHealth.IsDead) return;
        StartCoroutine(LandingEffect(center));

        SetIgnorePlayer(false);
        passage.Restore();
        timer = 0f;
        state = State.Recovery;
    }

    private System.Collections.IEnumerator LandingEffect(Vector2 center)
    {
        effectsRoot=new GameObject(name+" Landing Warning");
        var pulse=StoneGolemTelegraph.Create(effectsRoot.transform,warningMaterial,center,Vector2.right,attackRadius,360,0,false,new Color(.65f,.5f,.3f),0,1);
        var pieces=new SpriteRenderer[4]; var source=body.GetComponentInChildren<SpriteRenderer>();
        for(int i=0;i<4;i++) { var piece=new GameObject("Landing dust").AddComponent<SpriteRenderer>(); piece.transform.SetParent(effectsRoot.transform,false);piece.transform.position=center;piece.transform.localScale=Vector3.one*.12f;piece.sprite=source.sprite;piece.sortingOrder=2;pieces[i]=piece; }
        float t=0;while(t<.25f) { pulse.SetProgress(t/.25f);for(int i=0;i<4;i++) { float a=i*Mathf.PI*.5f; pieces[i].transform.position=(Vector3)center+new Vector3(Mathf.Cos(a),Mathf.Sin(a))*t*2;pieces[i].color=new Color(.65f,.5f,.3f,.4f*(1-t/.25f)); }yield return null;t+=Time.deltaTime; }
        if(effectsRoot) Destroy(effectsRoot); effectsRoot=null;
    }

    private void SetIgnorePlayer(bool ignore)
    {
        if (ignoringPlayer == ignore)
            return;

        if (ownCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(
                ownCollider, playerCollider, ignore
            );

        ignoringPlayer = ignore;
    }

    private void ResetVisuals()
    {
        if(effectsRoot) { effectsRoot.SetActive(false); Destroy(effectsRoot); } effectsRoot=null; telegraph=null;
        if (body != null)
            body.localPosition = bodyStartPosition;

        if (attackWarning != null)
            attackWarning.gameObject.SetActive(false);
    }

    public void CancelAttacks()
    {
        StopAllCoroutines();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        ResetVisuals(); SetIgnorePlayer(false);
        passage.Restore();
        state = State.Chase; timer = 0f;

    }
    private void OnTargetDied(PlayerHealth _) { CancelAttacks(); }
    private void OnDisable()
    {
        CancelAttacks();
        if (playerHealth != null) playerHealth.Died -= OnTargetDied;
    }
    private void OnEnable()
    {
        if (playerHealth != null) { playerHealth.Died -= OnTargetDied; playerHealth.Died += OnTargetDied; }
    }
    public void SetTarget(PlayerHealth target)
    {
        if (playerHealth != null) playerHealth.Died -= OnTargetDied;
        CancelAttacks();
        playerHealth = target;
        player = target != null ? target.transform : null;
        playerCollider = target != null ? target.GetComponent<Collider2D>() : null;
        if (playerHealth != null && isActiveAndEnabled) playerHealth.Died += OnTargetDied;
    }
}
