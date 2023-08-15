using System.Collections.Generic;
using UnityEngine;

public sealed class BulletManager : MonoBehaviour {

    private class BulletPool {
        public Bullet[] lst  = new Bullet[200];
        public int activeNum = 0;
        public int instNum   = 0;
    };
    private struct BulletPtr {
        public BulletPool pool;   // the addressing mode is [pool:offset].
        public int        offset; 
    };
    private struct List<T> {
        public T[] lst;
        public int cnt;
    };


    //////////////////////
    // Properties       //
    //////////////////////


    private static BulletManager Inst = null; // the single-ton instance

    private BulletPool mBullets = new BulletPool(); // the allocator to get a bullet with no `Rigidbody2D`.
    private BulletPool mBodies  = new BulletPool(); // the allocator to get a bullet with `Rigidbody2D`.

    private List<BulletPtr>            mLookup0 = new List<BulletPtr>();            // get `BulletPtr` by the instance id of gameObject.
    private Dictionary<int, BulletPtr> mLookup1 = new Dictionary<int, BulletPtr>(); // get `BulletPtr` by the bullet id.
    private List<Bullet>           mPendingKill = new List<Bullet>();               // the pending queue for the bullets confirmed destruction.

    private int mEffectLayer; // the layer index for the effect bullet

    [Header("General Settings")]
    public GameObject _bulletPrefab = null;
    public Rect       _bulletScope  = default;



    ///////////////////////
    // Private Methods   //
    ///////////////////////


    // Awake() Method
    private void Awake() {
        if (Inst == null) {
            Inst             = this;
            mLookup0.lst     = new BulletPtr[200];
            mLookup0.cnt     = 0;
            mPendingKill.lst = new Bullet[200];
            mPendingKill.cnt = 0;

            if (_bulletScope.size.magnitude == 0f) {
                Vector2 s = new Vector2(Screen.width, Screen.height);
                Vector2 w = Camera.main.ScreenToWorldPoint(s);
                w.x += 2f;
                w.y += 2f;
                _bulletScope = new Rect(-w.x, -w.y, w.x * 2, w.y * 2);
            }
            mEffectLayer = LayerMask.NameToLayer("Effect");

            if (mEffectLayer == -1) {
                Debug.LogError("BulletManager could not find \"Effect\" Layer");
            }
            Physics2D.SetLayerCollisionMask(mEffectLayer, 0); // the collider with "Effect" layer can't collide with any other collider.
            DontDestroyOnLoad(gameObject);
            return;
        }
        Destroy(gameObject);
    }



    // LateUpdate() Method
    private void LateUpdate() {

        for (int i = 0, cnt = mPendingKill.cnt; i < cnt; ++i) {
            Bullet        target   = mPendingKill.lst[i];
            ref BulletPtr ptr      = ref mLookup0.lst[target.GetBulletID()];
            Bullet[]      pool     = ptr.pool.lst;
            int           targetId = ptr.offset;
            int           fillerId = --ptr.pool.activeNum;
             
            Bullet filler  = pool[fillerId]; // temp
            pool[fillerId] = target;         // swap `target` and `filler`
            pool[targetId] = filler;         // the instance of `target` is returned to `BulletManager` 

            ref BulletPtr pfiller = ref mLookup0.lst[filler.GetBulletID()];
            pfiller.offset        = targetId;
            mLookup1[filler.gameObject.GetInstanceID()] = pfiller;
        }
        mPendingKill.cnt = 0;
    }


    // AllocBullet() Method
    private Bullet AllocBullet() {
        if (mBullets.activeNum == mBullets.instNum) {
            int capacity0 = mBullets.lst.Length;
            int capacity1 = mLookup0.lst.Length;

            GameObject clone   = Instantiate(_bulletPrefab);
            Bullet     newInst = clone.GetComponentInChildren<Bullet>();

            // when the capacity of `mBullets` is insufficent..
            if (mBullets.instNum == capacity0) {
                Bullet[] old = mBullets.lst;
                mBullets.lst = new Bullet[capacity0 * 2];
                old.CopyTo(mBullets.lst, 0);
            }

            // when the capacity of `mLookup0` is insufficent..
            if (mLookup0.cnt == capacity1) {
                BulletPtr[] old = mLookup0.lst;
                mLookup0.lst    = new BulletPtr[capacity1 * 2];
                old.CopyTo(mLookup0.lst, 0);
            }
            mLookup0.lst[mLookup0.cnt++].pool = mBullets;              // add `BulletPtr` to `mLookup0` for `newInst`
            mLookup1.Add(newInst.gameObject.GetInstanceID(), default); // add `BulletPtr` to `mLookup1` for `newInst`
            mBullets.lst[mBullets.instNum++] = newInst;                // assign `newInst` to `mBullets`
            DontDestroyOnLoad(clone);                                  // `clone` is never destroyed.
        }
        Bullet newBullet  = mBullets.lst[mBullets.activeNum];
        int    bulletId   = newBullet.GetBulletID();
        int    instanceId = newBullet.gameObject.GetInstanceID();

        mLookup0.lst[bulletId].offset = mBullets.activeNum++;
        mLookup1[instanceId]          = mLookup0.lst[bulletId];
        return newBullet;
    }


    // AllocBullet2() Method
    private Bullet AllocBullet2() {
        if (mBodies.activeNum == mBodies.instNum) {
            int capacity = mBodies.lst.Length;

            // when the capacity of `mBodies` is insufficent..
            if (mBodies.instNum == capacity) {
                Bullet[] old = mBodies.lst;
                mBodies.lst  = new Bullet[capacity * 2];
                old.CopyTo(mBodies.lst, 0);
            }


            // when `mBullets` have any extra instance of `Bullet`.
            if (mBullets.activeNum < mBullets.instNum) {
                Bullet      moved = mBullets.lst[--mBullets.instNum];  // take away the instance in `mBullets`.
                Rigidbody2D body  = moved.gameObject.AddComponent<Rigidbody2D>();

                body.gravityScale = 0f;
                body.sleepMode    = RigidbodySleepMode2D.NeverSleep; 

                mLookup0.lst[moved.GetBulletID()].pool = mBodies;
                mBodies.lst[mBodies.instNum++]         = moved;
            }

            else {
                GameObject  clone   = Instantiate(_bulletPrefab); 
                Bullet      newInst = clone.GetComponentInChildren<Bullet>();
                Rigidbody2D body    = newInst.gameObject.AddComponent<Rigidbody2D>();
                int       lookupCap = mLookup0.lst.Length;

                // the capacity of `mLookup0` is insufficent..
                if (mLookup0.cnt == lookupCap) {
                    BulletPtr[] old = mLookup0.lst;
                    mLookup0.lst    = new BulletPtr[lookupCap * 2];
                    old.CopyTo(mLookup0.lst, 0);
                }

                body.gravityScale = 0f;
                body.sleepMode    = RigidbodySleepMode2D.NeverSleep;

                mLookup0.lst[mLookup0.cnt++].pool = mBodies;                // add `BulletPtr` to `mLookup`  for `newInst`
                mLookup1.Add(newInst.gameObject.GetInstanceID(), default);  // add `BulletPtr` to `mLookup1` for `newInst
                mBodies.lst[mBodies.instNum++] = newInst;                   // assign `newInst` to `mBodies`.
                DontDestroyOnLoad(clone);                                   // the `clone` is never destroyed
            }
        }
        Bullet newBullet  = mBodies.lst[mBodies.activeNum];
        int    bulletId   = newBullet.GetBulletID();
        int    instanceId = newBullet.gameObject.GetInstanceID();

        mLookup0.lst[bulletId].offset = mBodies.activeNum++;
        mLookup1[instanceId]          = mLookup0.lst[bulletId];
        return newBullet;
    }



    // UpdateBullet() Method
    private void UpdateBullet(Bullet thisBullet, float deltaTime) {

        if (thisBullet.transform.gameObject.activeSelf) { // update if only the root gameObject is activated

            if (thisBullet.gameObject.layer == mEffectLayer) {
                thisBullet.animator.speed = 1f;
                thisBullet.animator.Update(deltaTime);
                thisBullet.animator.speed = 0f;

                if(!thisBullet.transform.gameObject.activeSelf) { // when the `DestroyThisBullet` Animation Event is invoked,
                    return;                                       // prevent the update in nextline
                }
            }
            if (thisBullet.onUpdate == null) {
                BehaveDefault(thisBullet, deltaTime); // do the default behaviour
                return;
            }
            thisBullet.onUpdate(thisBullet); // when it have a intelligence
        }
    }


    // IsValid() Method
    private bool IsValid(Bullet b) {
        return b.gameObject.layer != Inst.mEffectLayer;
    }


    ///////////////////////
    // Public Methods    //
    ///////////////////////


    // CreateBullet() Method
    public static Bullet CreateBullet(Vector2             position,
                                      Vector2             lookAt,
                                      float               speed,
                                      string              updateAnim    = null,
                                      string              destroyAnim   = null,
                                      Bullet.Intelligence onUpdate      = null,
                                      Bullet.Intelligence onTrigger     = null,
                                      Bullet.Intelligence onDestroy     = null,
                                      bool                withRigidbody = false) 
    {
        Bullet newBullet = withRigidbody ? Inst.AllocBullet2() : Inst.AllocBullet();

        newBullet.animator.speed = 1f;
        newBullet.transform.gameObject.SetActive(true); // activate the root gameObject of `newBullet`
        newBullet.animator.Play(updateAnim);

        newBullet.transform.localScale    = Vector3.one;
        newBullet.transform.localRotation = Quaternion.identity;
        newBullet.gameObject.layer        = 0;
        newBullet.triggerOrder            = 0;

        newBullet.transform.position = position;
        newBullet.lookAt             = lookAt;
        newBullet.speed              = speed;
        newBullet.destroyAnim        = destroyAnim;
        newBullet.onUpdate           = onUpdate;
        newBullet.onTrigger          = onTrigger;
        newBullet.onDestroy          = onDestroy;

        return newBullet;
    }



    // DestroyBullet() Method
    public static void DestroyBullet(Bullet target) {
        if (target.transform.gameObject.activeSelf) { // checks if the root gameObject of `target` is activated..

            if (target.destroyAnim != null) {
                string destroyAnim = target.destroyAnim;

                target.onDestroy?.Invoke(target); // call the destructor of `target`, 
                target.onUpdate         = null;   // then recycles the instance of `target` immediately
                target.onDestroy        = null;
                target.onTrigger        = null;
                target.destroyAnim      = null;
                target.gameObject.layer = Inst.mEffectLayer;
                target.speed            = 0f;  // for `Animator.Update`.
                target.animator.speed   = 0f;
                target.animator.Play(destroyAnim);
                return;
            }
            int capacity = Inst.mPendingKill.lst.Length;

            // the capacity of `mPendingKill` is insufficent..
            if (Inst.mPendingKill.cnt == capacity) {
                Bullet[] old          = Inst.mPendingKill.lst;
                Inst.mPendingKill.lst = new Bullet[capacity * 2];
                old.CopyTo(Inst.mPendingKill.lst, 0);
            }
            Inst.mPendingKill.lst[Inst.mPendingKill.cnt++] = target; // the bullet confirmed destruction is queued to the pending queue, then
            target.transform.gameObject.SetActive(false);            // the instance will be returned in `LateUpdate`.
            target.gameObject.layer = Inst.mEffectLayer;             // for removing the number of branches in `ForEach`, `GetBulletAll`
            target.onDestroy?.Invoke(target);                        // call the destructor of `target`.
        }
    }



    // DestroyBulletAll() Method
    public static void DestroyBulletAll() {

        while ((Inst.mBullets.activeNum + Inst.mBodies.activeNum) != 0) {

            for (int i = 0, cnt = Inst.mBullets.activeNum; i < cnt; ++i) {
                Bullet thisBullet = Inst.mBullets.lst[i];
                DestroyBullet(thisBullet);
            }
            for (int i = 0, cnt = Inst.mBodies.activeNum; i < cnt; ++i) {
                Bullet thisBullet = Inst.mBodies.lst[i];
                DestroyBullet(thisBullet);
            }
            Inst.LateUpdate(); // return their instances to `BulletManager`..
        }
    }


    // GetBullet() Method (overload 1)
    public static Bullet GetBullet(GameObject gameObject) {
        int key = gameObject.GetInstanceID();

        if (Inst.mLookup1.ContainsKey(key)) {
            BulletPtr ptr = Inst.mLookup1[key];
            return ptr.pool.lst[ptr.offset];
        }
        return null;
    }


    // GetBullet() Method (overload 2)
    public static Bullet GetBullet(int bulletId) {
        if (bulletId < Inst.mLookup0.cnt) {
            ref BulletPtr ptr = ref Inst.mLookup0.lst[bulletId];
            return ptr.pool.lst[ptr.offset];
        }
        return null;
    }


    // GetBulletAll() Method (overload 1)
    public static int GetBulletAll(Bullet[] result) {
        int ret = 0;

        for(int i=0, cnt=Inst.mBullets.activeNum; i<cnt; ++i) {
            Bullet b = Inst.mBullets.lst[i];
            if(b.gameObject.layer != Inst.mEffectLayer) result[ret++] = b;
        }
        for (int i = 0, cnt = Inst.mBodies.activeNum; i < cnt; ++i) {
            Bullet b = Inst.mBodies.lst[i];
            if (b.gameObject.layer != Inst.mEffectLayer) result[ret++] = b;
        }
        return ret;
    }


    // GetBulletAll() Method (overload 2)
    public static int GetBulletAll(bool withRigidbody, Bullet[] result) {
        BulletPool pool = withRigidbody ? Inst.mBodies : Inst.mBullets;
        int        ret  = 0;
        
        for(int i=0, cnt = pool.activeNum; i<cnt; ++i) {
            Bullet b = pool.lst[i];
            if(b.gameObject.layer != Inst.mEffectLayer) result[ret++] = b;
        }
        return ret;
    }



    // ForEach() Method (overload 1)
    public static void ForEach(Bullet.Intelligence operation) {
        
        for (int i = 0, cnt = Inst.mBullets.activeNum; i < cnt; ++i) {
            Bullet thisBullet = Inst.mBullets.lst[i];
            if (thisBullet.gameObject.layer != Inst.mEffectLayer) operation(thisBullet);
        }
        for (int i = 0, cnt = Inst.mBodies.activeNum; i < cnt; ++i) {
            Bullet thisBullet = Inst.mBodies.lst[i];
            if (thisBullet.gameObject.layer != Inst.mEffectLayer) operation(thisBullet);
        }
    }


    // ForEach() Method (overload 2) 
    public static void ForEach(bool withRigidbdoy, Bullet.Intelligence operation) {
        BulletPool pool = withRigidbdoy ? Inst.mBodies : Inst.mBullets;

        for (int i = 0, cnt = pool.activeNum; i < cnt; ++i) {
            Bullet thisBullet = pool.lst[i];
            if (thisBullet.gameObject.layer != Inst.mEffectLayer) operation(thisBullet);
        }
    }


    // Update() Method
    public static void Update(float deltaTime) {

        for (int i = 0, cnt = Inst.mBullets.activeNum; i < cnt; ++i) {
            Bullet thisBullet = Inst.mBullets.lst[i];
            Inst.UpdateBullet(thisBullet, deltaTime);
        }
        for (int i = 0, cnt = Inst.mBodies.activeNum; i < cnt; ++i) {
            Bullet thisBullet = Inst.mBodies.lst[i];
            Inst.UpdateBullet(thisBullet, deltaTime);
        }
    }


    // BehaveDefault() Method
    public static void BehaveDefault(Bullet thisBullet, float deltaTime) {
        Vector2 newPos = thisBullet.transform.position += (Vector3)thisBullet.lookAt * (thisBullet.speed * deltaTime);

        if (!Inst._bulletScope.Contains(newPos)) {
            DestroyBullet(thisBullet);
        }
    }


    // bulletScope getter/setter
    static public Rect bulletScope {
        get { return Inst._bulletScope; }
        set { Inst._bulletScope = value; }
    }


    // bulletPrefab getter/setter
    static public GameObject bulletPrefab {
        get { return Inst._bulletPrefab; }
        set { if (Inst._bulletPrefab == null) Inst._bulletPrefab = value; }
    }


    // effectLayer getter
    static public int effectLayer {
        get { return Inst.mEffectLayer; }
    }


    // instanceCount getter
    static public int instanceCount {
        get { return Inst.mLookup0.cnt; }
    }


    // bulletCount getter
    static public int bulletCount {
        get { return Inst.mBullets.activeNum;}
    }


    // bodyCount getter
    static public int bodyCount {
        get { return Inst.mBodies.activeNum; }
    }
}
