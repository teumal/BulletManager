using UnityEngine;


[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(LineRenderer))]
public class Bullet : MonoBehaviour {

    public delegate void Intelligence(Bullet thisBullet, Collider2D collision = null);

    public struct RegisterSet {
        public float f1, f2, f3, f4;
        public int   i1, i2, i3, i4;
    };

    ///////////////////////
    // Properties        //
    ///////////////////////
    
    private int mBulletID;

    [HideInInspector] public MobBase shooter;
    [HideInInspector] public MobBase target;
    [HideInInspector] public MobBase.MobStat stat;

    [HideInInspector] public Vector2 lookAt;
    [HideInInspector] public float   speed;
    [HideInInspector] public string  destroyAnim;

    public Intelligence onUpdate;
    public Intelligence onTrigger;
    public Intelligence onCollision;
    public Intelligence onDestroy;
    public RegisterSet  registers;

    public     Animator        animator       { get; private set; }
    public     SpriteRenderer  spriteRenderer { get; private set; }  
    public     LineRenderer    lineRenderer   { get; private set; }
    new public Collider2D      collider       { get; private set; }

    [HideInInspector] new public Rigidbody2D rigidbody;

    new public Transform transform { 
        get { return gameObject.transform.root; } 
    }

    public int triggerOrder = 0;

    ///////////////////////
    // Private Methods   //
    ///////////////////////

    // Awake() Method
    private void Awake() {
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lineRenderer   = GetComponent<LineRenderer>();

        collider           = GetComponent<Collider2D>();
        collider.isTrigger = true;
        mBulletID          = BulletManager.instanceCount;
    }


    // OnCollisionEnter2D() Method
    private void OnCollisionEnter2D(Collision2D collision) {
        if (gameObject.layer == BulletManager.effectLayer) { // when the layer of this gameObject is "Effect",
            return;                                          // thisBullet is confirmed destruction or for effect.
        }
        onCollision?.Invoke(this, collision.collider);
    }


    //////////////////////////
    // Public Methods       //
    //////////////////////////

    // OnTriggerStay2D() Method
    public void OnTriggerStay2D(Collider2D collision) {

        if (gameObject.layer == BulletManager.effectLayer) { // when the layer of this gameObject is "Effect",
            return;                                          // thisBullet is confirmed destruction or for effect.
        }
        Bullet other = BulletManager.GetBullet(collision.gameObject);

        if (other != null && triggerOrder < other.triggerOrder) {
            return;
        }
        if (onTrigger == null) {
            BulletManager.DestroyBullet(this);
            return;
        }
        onTrigger(this, collision);
    }


    // OnCollisionEnter2D() Method
    private void OnCollisionEnter2D(Collider2D collision) {
        if (gameObject.layer == BulletManager.effectLayer) { // when the layer of this gameObject is "Effect",
            return;                                          // thisBullet is confirmed destruction or for effect.
        }
        onCollision?.Invoke(this, collision);
    }


    // DestroyThisBullet() Method
    public void DestroyThisBullet() {
        BulletManager.DestroyBullet(this);
    }


    // BehaveDefault() Method
    public void BehaveDefault(float deltaTime) {
        BulletManager.BehaveDefault(this, deltaTime);
    }

    // GetBulletID() Method
    public int GetBulletID() {
        return mBulletID;
    }
}

