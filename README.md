# BulletManager

## Overview
2D 탑뷰 시점 슈팅 게임에서 총알을 관리하기 위한 script component 입니다. `Bullet`, `BulletManager`, `MobBase` 이렇게 세 개의 C# script 가 존재합니다. 여기서 `MobBase.cs`는 필수가 아닙니다. <br><br>

# Tutorial
## 1. 프리팹의 생성과 등록
`BulletManager`를 사용하기 위해, 가장 먼저 해야할 것은 `Bullet` 컴포넌트를 부착한 프리팹(Prefab)을 준비하는 것입니다. 
``` c#
[ RequireComponent( typeof (Collider2D) )]
[ RequireComponent( typeof (Animator) )]
[ RequireComponent( typeof (SpriteRenderer) )]
[ RequireComponent( typeof (LineRenderer) )]
public class Bullet : MonoBehaviour;
```
기본적으로 `Bullet` 컴포넌트를 부착하기 위해서는 `Collider2D`, `Animator`, `SpriteRenderer`, `LineRenderer` 이렇게 4개의 컴포넌트가 필수로 부착되어 있어야 합니다. 해당 컴포넌트들은 C# script 에서 각각 `collider`, `animator`, `spriteRenderer`, `lineRenderer` 라는 이름으로 얻어올 수 있습니다. <br><br>

- `Collider2D` 컴포넌트는 `BoxCollider2D`, `CircleCollider2D`, `CapsuleCollider2D` 중 하나를 정해 부착하도록 합니다. 일반적으로 총알의 콜라이더로는 `BoxCollider2D`를 추천합니다. 사용할 스프라이트(sprite)에 따라 유연하게 범위를 조정할 수 있기 때문입니다. <br><br>

- `LineRenderer` 컴포넌트는 총알의 꼬리를 그리거나, 레이저의 기둥을 표현하기 위해 사용됩니다. 해당 컴포넌트는 후술할 총알의 지능 `Bullet.Intelligence`에서 사용됩니다. <br><br>

- `Animator`, `SpriteRenderer` 컴포넌트는 총알의 외형과 애니메이션을 결정하기 위해 사용됩니다. 예를 들어, `"Breath"` 라는 이름의 animation clip 을 만든다고 합시다. 그러면 `SpriteRenderer.sprite`, `CircleCollider2D.radius`, `Transform.localScale` 등을 적절하게 수정하여 다음과 같이 애니메이션을 만들어주면 됩니다:

<img align=center src="https://github.com/teumal/BulletManager/blob/main/breath%20example.gif?raw=true">

여기서는 편의를 위해, `Transform.localScale`을 애니메이션에서 수정했습니다. 문제가 있다면 이렇게 건들인 속성들은 `Animator`가 매번 값을 덮어씌우기에, C# 스크립트에 `Transform.localScale`을 수정하기가 힘들어집니다는 점입니다. 이에 대한 해결책으로 `Bullet`이 부착된 GameObject를 자식으로 넣는 것을 추천합니다:
<table>
  <tr>
    <td><img src="https://github.com/teumal/BulletManager/blob/main/BulletManager,%20Bullet/image/parent.JPG?raw=true"></td>
    <td><img src="https://github.com/teumal/BulletManager/blob/main/BulletManager,%20Bullet/image/child.JPG?raw=true"></td>
  </tr>
  <tr>
    <td align=center>Parent</td>
    <td align=center>Child</td>
  </tr>
</table>

이제 부모의 `Transform.localScale`을 수정하는 것으로 위 문제를 해결할 수 있습니다. <br><br>

- 이렇게 만든 프리팹은 앞으로 만들 모든 커스텀 총알의 기반이 됩니다. 그렇기에 위 4개의 컴포넌트 말고도 프로젝트의 목적에 맞게끔, `ParticleSystem`, `TrailRenderer` 등의 컴포넌트들을 추가로 부착할 수 있습니다. `GetComponent<T>()` 함수로 얻은 결과를 캐싱하여 사용할 수 있도록, `Bullet` 스크립트를 수정하는 것 또한 허용됩니다:
``` c#
class Bullet {
  ...
  public TrailRenderer trailRenderer;  // 새로 추가

  // Awake() Method
    private void Awake() {
        animator       = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lineRenderer   = GetComponent<LineRenderer>();
        trailRenderer  = GetComponent<TrailRenderer>(); // 새로 추가

        collider           = GetComponent<Collider2D>();
        collider.isTrigger = true;
        mBulletID          = BulletManager.instanceCount;
    }
};
```
다만, `Rigidbody2D` 컴포넌트는 예외입니다. 일반적으로 많은 총알들이 `Rigidbody2D`를 필요로하지 않으며, 한쪽만 `Rigidbody2D`를 가지고 있어도, `OnTriggerXXX`는 호출되기 때문입니다. 물론, 총알이 `Rigidbody2D`가 필요한 경우는 얼마든지 있을 수 있습니다. 이 경우, `BulletManager`가 `Rigidbody2D`를 총알에 부착하게 됩니다. 자세한 것은 `BulletManager.CreateBullet()` 의 레퍼런스를 참고하시길 바랍니다.

- `Bullet.shooter`, `Bullet.target`, `Bullet.stat` 속성의 경우 **Implementation defined** 입니다. 기본적으로 `MobBase` 타입으로 되어있지만, 프로젝트에 맞게끔 적절한 타입으로 정의하여 사용하거나 제거하는 것을 허용합니다. 자세한 것은 `documentation.html`을 참고하시길 바랍니다.

이렇게 프리팹을 알맞게 만들었다면, 인스펙터(Inspector) 또는 C# 스크립트를 통해 `BulletManager.bulletPrefab` 속성에 등록해줍니다. 프리팹은 하나만 등록할 수 있으며, 이후에는 수정할 수 없습니다. 내부적으로 프리팹이 섞이는 것을 방지하기 위함입니다. 이제 `BulletManager`를 사용할 준비는 끝났습니다.<br><br>


## 2. 총알의 생성,파괴, 갱신
`Bullet`의 생성,파괴,갱신은 항상 `BulletManager`의 메소드를 통해 수행해야 합니다. 그렇지 않으면 결과는 **Undefined behavior** 입니다. 각각의 역할을 수행하는 메소드들로 `CreateBullet()`, `DestroyBullet()`, `Update()` 가 있습니다. 인자에 대해서는 `documentation.html`을 참고하시길 바랍니다. 한번 처음에 주어진 위치에서 똑같은 방향과 속도로 쭉 날아가다, 화면을 벗어나면 파괴되는 총알을 만들어봅시다.
``` c#
void Start() { // Awake()가 아니라, Start()임에 주목합시다.
   BulletManager.CreateBullet(Vector2.zero, Vector2.up, 2f, "Default", "Explosion"); // 총알을 하나 생성.
}
void Update() {
   BulletManager.Update(Time.detlaTime); // 생성된 모든 총알들의 로직을 갱신함.
}
```
위 예제는 `Vector2.zero`에서 생성되어서, `Vector2.up`의 방향으로 1초에 `2f` 만큼 이동하는 총알을 생성합니다. 또한 총알은 `"Default"` 이라는 애니메이션을 사용하며, 파괴될 시점에서 `"Explosion"` 애니메이션이 실행되게 됩니다. 여기서 파괴될 시점은 화면을 벗어나거나, 무언가와 부딪힌 경우를 의미합니다. <br><br>

여기서 "Explosion" 과 같은 animation clip 들은 항상 애니메이션의 끝에 `DestroyThisBullet()` 을 animation event 로 사용해야 합니다. 또한, 총알은 자동을 갱신되지 않는 것을 알 수 있습니다. 고로 `BulletManager.Update(Time.deltaTime)` 처럼 직접 `Update()` 에서 호출을 해주어야 한다는 의미입니다. 이와 관련해서 자세한 것은 `documentation.html`을 참고하시길 바랍니다. <br><br>

총알에 지능을 주지 않으면, 처음에 주어진 위치(position)에서 정해진 방향(lookAt)과 속도(speed)로 날라가다가 화면 밖으로 나갈 경우, 파괴되는 지능이 없는 총알을 생성합니다. 이를 기본동작(default beahviour)이라고 합니다. 사용자 정의 동작(user-defined behaviour)을 수행하는 총알을 만들고 싶다면 총알에 지능을 주어야 합니다. 

<br><br>

## 3. 총알의 지능
총알은 단순한 발사체(projectile)가 아닙니다. `onUpdate`, `onTrigger`, `onCollision`, `onDestroy` 등의 총 4가지의 지능을 통해, 적을 추적하거나 총알이 총알을 소환하는 마치 적(enemy)으로서도 행동할 수 있으며, 레이저가 되거나, 서로의 총알을 튕겨내는 등 여러가지의 것들을 할 수 있습니다. 지능을 추가하기 위해서는 `tag`, `layer` 등의 여러가지 속성들을 초기화해야합니다. 이와 같은 추가 초기화(extra initialization)는 `BulletManager.CreateBullet()` 이 돌려주는 반환값을 통해 수행하시길 바랍니다. 한번 다음 2개의 예시를 통해, 어떻게 총알의 지능을 부여하고 다루는지 알아봅시다:


## 3.1. 간단한 레이저빔
``` shader
// LaserAnim.shader
// for Builtin Render Pipeline
Shader "Custom/LaserAnim" {

    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        Tags { "Queue" = "Transparent" }

        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;


            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target{
                float frame = round(_Time.y * 15.f);

                fixed4 col = tex2D(
                    _MainTex, float2(i.uv.x * 0.25f + frame * 0.25f, i.uv.y)
                );
                return col;
            }
            ENDCG
        }
    }
}
```
``` C#
// Example.cs
using UnityEngine;

public class Example : MonoBehaviour {
    public Material laserAnimMat; // 인스펙터에서 초기화

    // Start() Method
    private void Start() {
        CreateLaser(10f); // 레이저의 지속시간은 10초.
    }


    // Update() Method
    private void Update() {
        BulletManager.Update(Time.deltaTime);
    }


    // CreateLaser() Method
    private void CreateLaser(float duration) {
        Bullet newBullet = BulletManager.CreateBullet(default,default,default,"LaserHit");

        newBullet.lineRenderer.sharedMaterial = laserAnimMat;
        newBullet.lineRenderer.positionCount  = 2;
        newBullet.lineRenderer.startWidth     = 2f;
        newBullet.lineRenderer.endWidth       = 2f;
        newBullet.lineRenderer.sortingOrder   = -1;
        newBullet.gameObject.layer            = LayerMask.NameToLayer("Ignore Raycast");

        newBullet.registers.f1 = 0f;       // f1: timer
        newBullet.registers.f2 = duration; // f2: duration

        newBullet.lineRenderer.SetPosition(0, Vector2.zero);
        newBullet.lineRenderer.SetPosition(1, Vector2.zero);
        
        newBullet.onUpdate = (b, c) => {
            ref float timer     = ref b.registers.f1;
            ref float duration  = ref b.registers.f2;
            float     deltaTime = Time.deltaTime;

            // `duration` 초가 지나면, 레이저가 점점 약해지다 사라진다.
            if((timer += deltaTime) >= duration) {
                b.lineRenderer.startWidth = b.lineRenderer.endWidth -= deltaTime * 0.9f;
                b.transform.localScale    = b.transform.localScale - new Vector3(deltaTime * 0.45f, 0f, 0f);
                
                if(b.lineRenderer.startWidth < 0.1f) {
                    b.DestroyThisBullet();
                    return;
                }
            }
            Vector2      mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2      direction     = mousePosition.normalized;
            RaycastHit2D hit           = Physics2D.BoxCast(Vector2.zero, Vector2.one, 0f, direction);
            float        zAngle        = 0f;
           
            if     (hit.collider.name=="LeftWall")  zAngle = -90f;
            else if(hit.collider.name=="RightWall") zAngle = 90f;
            else if(hit.collider.name=="TopWall")   zAngle = -180f;

            b.transform.localRotation = Quaternion.Euler(0f,0f, zAngle);
            b.transform.position      = hit.point - direction * 0.5f; // (direction * 0.5f) 는 위치를 조금 조정하는 용도.
            b.lineRenderer.SetPosition(0, hit.point);
        };
        newBullet.onDestroy = (b, c) => {
            b.lineRenderer.sharedMaterial = null;
            b.lineRenderer.positionCount  = 0;
            b.lineRenderer.sortingOrder   = 0;
        };
    }
}
```
<img src="https://github.com/teumal/BulletManager/blob/main/laser%20example.gif?raw=true">

위 예제는 원점 `Vector2.zero`에서, 마우스 커서를 향해 발사되는 레이저를 구현합니다. 화면은 `LeftWall`, `RightWall`, `TopWall`, `BottomWall` 이라는 4개의 벽으로 사방이 막혀있기에, 레이저는 항상 4개의 벽중 하나에 부딪힌다고 가정합니다. 이렇게 `CreateLaser()` 메소드로 생성된 레이저는 인자로 준 `duration` 의 시간이 지나면서 자연스럽게 사라집니다. <br><br>

위 코드는 총알의 지능을 다음의 규칙(convention)을 따라 작성하였습니다:
- `onUpdate` 와 같은 지능들은 기본적으로 `lambda expression` 을 사용했으며, 그 인자는 항상 `(b,c)=>{}` 와 같은 식입니다. 람다식의 인자는 각각 `Bullet thisBullet`, `Collider2D collision` 을 의미합니다. `c`는 `onTrigger`, `onCollision` 인 경우에만 `c != null` 입니다. 

- `Bullet.registers`가 여유롭다면, 가능한 람다식의 캡처를 사용하지 않도록 합니다. 이는 람다식이 항상 박싱(Boxing)되는 것을 방지하기 위함입니다. 여기서는 `f1`을 `timer`로, `f2`를 `duration`을 사용했습니다. 만약 여유 레지스터들이 부족하다면, 필요한 변수를 캡처합니다.

- `LineRenderer.positionCount`와 같이 정리를 해주어야하는 자원들은 `onDestroy` 지능에서 디폴트(default)값으로 초기화해줍니다.

- `b.DestroyThisBullet()`을 호출했다면, 그 다음에는 바로 `return`을 해줍니다.

`Bullet.RegisterSet`는 아주 귀중한 자원이므로 가능하다면 최소한으로 쓸 수 있도록 해야합니다. 기본적으로 **floating-point** 를 위한 레지스터 변수 `f1, f2, f3, f4`. **integral number**를 위한 레지스터 변수 `i1, i2, i3, i4`가 제공됩니다. 프로젝트에 따라, 레지스터의 갯수를 줄이거나 늘리는 것을 허용합니다. 물론, 모든 총알의 크기가 커질 수 있음에 유의하시길 바랍니다. 상황에 따라서는 `Bullet.lookAt`, `Bullet.speed` 또한 레지스터 변수로서 사용할 수 있지만, 권장하지는 않습니다. <br><br>

위 코드에서 레이저의 기둥을 표현하기 위해서 `LineRenderer`를 사용했습니다. `lineRenderer.sharedMaterial` 을 사용했는데, 이는 기존의 `lineRenderer.material`이 항상 새로운 `Material` 인스턴스를 만들어내기 때문입니다. Render Pipeline 에 따라서 `MaterialPropertyBlock` 을 고려해볼 수 있겠습니다. 레이저 기둥의 애니메이션은 위의 셰이더 코드처럼 UV 애니메이션으로 처리합니다. <br><br>

예제에서는 벽과 총알 모두 `Rigidbody2D` 가 부착되어 있지 않습니다. 그렇기에 `onTrigger` 또한 호출되지 않았습니다. 또한 `BoxCast` 에서는 `layerMask` 인자를 주지 않았기에 원래 총알 자기 자신 또한 `BoxCast`의 대상입니다. 여기서는 "Ignore Raycast" 레이어를 주었지만, 실제로는 `layerMask` 인자를 줘야 합니다. <br><br>

## 3.2. 캐치 볼
``` c#
using UnityEngine;

enum LayerType : int {
    Default,
    TransparentFX,
    IgnoreRaycast,
    Effect,
    Water,
    UI,
    PlayerAttack,
    Player,
    Enemy,
    EnemyAttack,
};


public class Player : MonoBehaviour {
    public static Camera mainCamera;

    public     GameObject  enemy;     
    new public Rigidbody2D rigidbody;
    public     Vector2     velocity;

    // Start() Method
    private void Start() {
        rigidbody  = GetComponent<Rigidbody2D>();
        mainCamera = Camera.main;

        CreateSword();
        CreateCatchBall(enemy, gameObject);
        Application.targetFrameRate = 60; // 에디터에서도 60프레임이 나오도록 조정
    }


    // Update() Method
    private void Update() {
        BulletManager.Update(Time.deltaTime);

        Vector2 force = new Vector2(
           Input.GetAxis("Horizontal"),
           Input.GetAxis("Vertical")
        );
        velocity += force * (Time.deltaTime * 8f);
    }


    // FixedUpdate() Method
    private void FixedUpdate() {
        rigidbody.MovePosition(rigidbody.position + velocity);
        velocity = Vector2.zero;
    }

    // CreateSword() Method
    private void CreateSword() {
        Bullet sword = BulletManager.CreateBullet(transform.position, default, default, "Static");

        sword.gameObject.layer = BulletManager.effectLayer; // 검은 어떤 물체와도 부딪히지 않음. LayerType.Effect
        sword.animator.speed   = 0f;                        // 애니메이션은 움직이지 않는다.
        sword.shooter          = gameObject;                // `GameObject shooter;` 로 정의돼있음.

        sword.registers.f1 = 0f; // f1: timer
        sword.registers.i1 = 1;  // i1: step

        sword.onUpdate = (b, c) => {
            ref float timer     = ref b.registers.f1;
            ref int   step      = ref b.registers.i1;
            float     deltaTime = Time.deltaTime;

            // step 1) 검이 마우스 커서를 향해 바라보게 하고, 마우스 좌클릭으로 공격을 시전
            if(step == 1) {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 direction     = (mousePosition - (Vector2)b.shooter.transform.position).normalized;
                float zAngle          = Vector2.SignedAngle(Vector2.right, direction);

                b.transform.localRotation     = Quaternion.Euler(0f, 0f, zAngle); // 마우스 커서의 방향으로 회전
                b.spriteRenderer.sortingOrder = direction.y < 0f ? 1 : -1;        // 각도에 따라 검이 플레이어에게 가려진다.

                if (Input.GetMouseButtonDown(0)) {
                    timer = 0f;
                    step  = 2;                       
                    b.transform.Rotate(0f, 0f, 75f);       // 검을 반시계방향으로 75도 회전
                    CreateSlash(zAngle, ref direction, b); // 참격을 생성.
                }
            }

            // step 2) 0.2 초가 지나면, 검을 시계방향으로 150도 회전
            else if(step == 2) {

                if((timer += deltaTime) > 0.2f) {
                    step = 3;
                    b.transform.Rotate(0f,0f,-150f);
                }
            }
            
            // step 3) 0.1초 동안, 검을 부드럽게 회전
            else {
                b.transform.Rotate(0f,0f, 750f * deltaTime);

                if((timer += deltaTime) > 0.3f) {
                    step = 1;
                }
            }

            b.transform.position = b.shooter.transform.position; // 검은 플레이어를 따라다닌다.
        };
    }


    // CreateSlash() Method
    private static void CreateSlash(float zAngle, ref Vector2 direction, Bullet sword) {
        Vector2 position = (Vector2) sword.transform.position + direction;
        Bullet  slash    = BulletManager.CreateBullet(position, default, default, "Slash", null, null, null, null, true);

        slash.gameObject.layer = (int) LayerType.PlayerAttack;
        slash.shooter          = sword.transform.gameObject;
        slash.transform.Rotate(0f,0f,zAngle);

        slash.lookAt = direction; // lookAt: direction

        slash.onUpdate = (b, c) => {
            ref Vector2 direction = ref b.lookAt;
            b.transform.position  = (Vector2) b.shooter.transform.position + direction;
        };
        slash.onTrigger = (b, c) => {
            b.gameObject.layer = BulletManager.effectLayer;
        };
    }


    // CreateEffect() Method
    private static void CreateEffect(Vector2 position, string updateAnim, int sortingOrder) {
        Bullet effect = BulletManager.CreateBullet(position, default, 0f, updateAnim);

        effect.spriteRenderer.sortingOrder = sortingOrder;
        effect.gameObject.layer            = BulletManager.effectLayer;
        effect.onDestroy                   = (b,c)=>{ b.spriteRenderer.sortingOrder = 0; };
    }


    // CreateCatchBall() Method
    private static void CreateCatchBall(GameObject shooter, GameObject target) {
        Bullet ball = BulletManager.CreateBullet(
           shooter.transform.position,
           (target.transform.position-shooter.transform.position).normalized,
           4f,
           null,
           "Explosion"
        );
        ball.shooter = shooter;
        ball.target  = target;

        ball.triggerOrder     = 1; // `CreateSlash()`로 생성한 Bullet의 onTrigger보다 우선순위를 높게한다.
        ball.gameObject.layer = (int) LayerType.EnemyAttack;
        ball.animator.speed   = 0f;
        ball.animator.Play("Static", 0, 1f);

        ball.registers.f1 = 0f;   // f1: timer
        ball.registers.f2 = 0.3f; // f2: interval
        ball.registers.i1 = 0;    // i1: hitCount

        ball.onUpdate = (b, c) => {
            ref float timer     = ref b.registers.f1;
            ref float interval  = ref b.registers.f2;
            float     deltaTime = Time.deltaTime;

            if((timer += deltaTime) > interval) {                 // interval 초마다 한번씩
                CreateEffect(b.transform.position, "Effect", -1); // 잔상을 생성한다.
                timer -= interval;
            }
            b.transform.Rotate(0f,0f,-800f * deltaTime); // 자전하면 날아간다.
            b.BehaveDefault(deltaTime);                  // 기본동작을 수행하여 움직인다.
        };
        ball.onTrigger = (b, c) => {
            ref float interval = ref b.registers.f2;
            ref int   hitCount = ref b.registers.i1;

            // 플레이어와 부딪히거나, 패스횟수가 20번을 넘어가면, 적은 받아치는데 실패함. 
            if(c.gameObject.layer == (int) LayerType.Player || hitCount > 20 && c.gameObject.layer == (int) LayerType.Enemy) {
                ShakeCamera(0.2f, 10);
                b.DestroyThisBullet();
                return;
            }
            ScaleTime(0.01f, 5);                              // 타격감을 위해, 시간을 느리게 한다.
            ShakeCamera(0.05f, 5);                            // 카메라 흔들림 효과
            CreateEffect(b.transform.position, "Reflect", 2); // 튕겨내는 효과를 생성한다.

            b.lookAt = (b.shooter.transform.position - b.target.transform.position).normalized; // 방향 반전
            interval = Mathf.Max(0.15f, interval - 0.0375f);                                    // 잔상효과의 생성간격이 점점 줄어든다. 최소 0.15초
            b.speed++;                                                                          // 패스할때마다 속도가 1f 씩 증가
            hitCount++;                                                                         // 패스 횟수를 기록한다.

            var temp  = b.shooter;
            b.shooter = b.target;
            b.target  = temp; // swap(b.shooter, b.target);

            if (c.gameObject.layer == (int)LayerType.PlayerAttack) {
                Bullet slash = BulletManager.GetBullet(c.gameObject);

                b.gameObject.layer = (int)LayerType.PlayerAttack; // `ball`은 이제 플레이어의 공격으로 취급.
                slash.onTrigger(slash, b.collider);               // 우선순위에 밀린 `slash.onTrigger`를 호출해준다. 
                return;
            }
            b.gameObject.layer = (int)LayerType.EnemyAttack;
        };
    }


    // ShakeCamera() Method
    private static void ShakeCamera(float shakePow, int loopCount=10) {  
        Bullet cameraShaker = BulletManager.CreateBullet(default,default,default,"None");

        cameraShaker.gameObject.layer = BulletManager.effectLayer;
        cameraShaker.lookAt           = Vector2.right;
        cameraShaker.registers.f1     = shakePow * (1f / loopCount); // f1: sub
        cameraShaker.registers.f2     = shakePow;                    // f2: shakePow
        cameraShaker.registers.f3     = 0.025f;                      // f3: delay
        cameraShaker.registers.i1     = 0;                           // i1: count

        cameraShaker.onUpdate = (b, c) => {
            ref float sub      = ref b.registers.f1;
            ref float shakePow = ref b.registers.f2;
            ref float delay    = ref b.registers.f3;
            ref int   count    = ref b.registers.i1;
            
            if(shakePow <= 0f) {       // shakwPow 가 바닥나면,
                b.DestroyThisBullet(); // cameraShaker의 인스턴스는 BulletManager가 회수한다.
                return;
            }

            if((delay += Time.deltaTime) > 0.025f) {                           
                mainCamera.transform.position += (Vector3)b.lookAt * shakePow; // 0.025 초마다 카메라를 이동.
                b.lookAt = new Vector2(b.lookAt.y, -b.lookAt.x);               // `b.lookAt`을 90도 회전.
                delay -= 0.025f;                                               // 타이머 초기화.

                if (++count == 4) {  // 상하좌우로 모두 이동할때마다
                    shakePow -= sub; // shakePow 를 감소시킨다.
                    count     = 0;
                }
            }
        };
    }


    // ScaleTime() Method
    private static void ScaleTime(float scaleRatio, int skipFrame) {

        if (Time.timeScale >= 1f) {

            Bullet timeScaler = BulletManager.CreateBullet(default, default, default, "None", null, (b, c) => {
                ref int frameCount = ref b.registers.i1;
                ref int skipFrame  = ref b.registers.i2;

                if (frameCount++ >= skipFrame) { // skipFrame 만큼의 프레임이 지나면,
                    Time.timeScale = 1f;         // 시간의 흐름을 원래대로 되돌린후
                    b.DestroyThisBullet();       // `timeScaler`의 인스턴스를 BulletManager가 회수한다.
                }
            });
            timeScaler.registers.i1 = 0;         // i1: frameCount
            timeScaler.registers.i2 = skipFrame; // i2: skipFrame

            Time.timeScale = scaleRatio; // scaleRatio 배 만큼 시간을 느리게 한다.
        }
    }
}
```
<img src="https://github.com/teumal/BulletManager/blob/main/catchball%20example.gif?raw=true">

이번 예제에서는 총알이 발사체(projectile) 이외의 것을 수행할 수 있다는 것을 확실히 보여줍니다. 한번 차례대로 살펴보도록 하겠습니다. 특히나 `BulletManager.effectLayer` 가 핵심입니다. 처음에 `BulletManager`를 사용하려고 하면, `"BulletManager could not find "Effect" Layer"` 라는 에러가 출력될 것입니다. 이는 **이팩트용 총알**을 구현하기 위해서, `BulletManager`가 `Effect` 라는 이름의 Layer 를 정의할 것을 요구하기 때문입니다. 이팩트용 총알은 단순히 `gameObject.layer == BulletManager.effectLayer` 인 총알을 의미하며, 어떠한 물체와도 충돌할 수 없는 총알을 의미합니다. 한번 위 코드를 분석해봅시다:<br><br>

- `CreateSword()` 메소드는 플레이어를 따라다니는 검을 생성합니다. 물론 검 또한 `Bullet` 입니다. 검은 플레이어에게 시각적인 정보만을 주는 용도이므로, `sword.gameObject.layer = BulletManager.effectLayer` 를 해주어 이팩트용 총알로 만들어 주었습니다. 특이하게도 `sword.animator.Play("Static", 0, 0f);` 처럼 해주었는데, `"Static"`이라는 animation clip 하나에 여러가지의 스프라이트들을 담아두기 위함입니다. 검은 공격할 방향을 알려주며, 마우스 좌클릭을 눌러 참격을 날리며, 검을 휘두르는 모션을 수행합니다. <br><br>

- `CreateSlash()` 메소드는 시각적인 용도인 검과 달리, 충돌판정이 필요하기에 `BulletManager.CreateBullet()`의 `withRigidbody=true` 처럼 인자를 전달해주었습니다. 이렇게 생성된 `slash` 객체는 `Rigidbody2D`가 부착되어 있습니다. 부착된 `Rigidbody2D`는 `BulletManager`에 의해 별도로 관리되므로 컴포넌트를 제거하지 마십시오. <br><br>주목할 것은 `b.lookAt`을 `Vector2` 타입의 레지스터 변수로 사용했다는 점과, `onUpdate`에 `b.DestroyThisBullet()`을 호출하지 않는다는 점입니다. 위에서도 언급했듯이, `b.lookAt` 을 레지스터 변수로 사용하는 것은 권장하지 않습니다. 누군가가 `slash.lookAt`을 방향벡터로 생각하고 수정할 수도 있기 때문입니다. 여기서는 편의를 위해 이렇게 해주었지만, 실제로는 `RegisterSet` 구조체를 수정해서 `Vector2 v1` 과 같은 레지스터 변수를 정의하거나, 벡터의 각 성분들을 `f1`, `f2` 에 따로 담아두는 것을 권장합니다. <br><br>
`slash.onUpdate`에는 `DestroyThisBullet()`을 호출하지 않는데, 이는 `"Slash"` animation clip 에서 animation event 로서 `DestroyThisBullet()` 을 호출하기 때문입니다. 이는 `"Effect"`, `"Explosion"` animation clip 또한 매한가지입니다. <br><br>

- `CreateCatchBall()` 메소드는 플레이어와 적이 주고받는 캐치볼을 생성합니다. `ball`은 `interval` 간격마다 `"Effect"`라는 애니메이션을 가진 잔상을 생성합니다. 조금 오버헤드가 있을 수 있지만, 잔상으로 사용된 `Bullet`은 금방 파괴되며 가까운 `BulletManager.CreateBullet()` 호출에서 높은 확률로 재사용되므로 편의성을 고려한 절충안으로 생각할 수 있습니다. <br><br> 여기서 핵심은 `triggerOrder` 속성입니다. A,B 라는 GameObject 가 있다고 할때, 누구의 `Update()`가 먼저 호출될까요?. 알 수 없지요. 총알이 다른 총알을 수정하는데 있어서 `OnTriggerXXX` event function 의 호출 순서가 매우 중요한 요소입니다. 이를 위해 `Bullet`은 `triggerOrder` 라는 속성을 제공합니다. 높을 수록 우선순위가 커지며, 우선순위가 낮은 총알의 `OnTriggerXXX`는 무시되도록 합니다. 해당 속성은 총알끼리의 충돌에서만 의미가 있으며, `onTrigger` 지능에서 `triggerOrder`를 수정하는 것은 **Undefined Behavior** 임을 알아두시길 바랍니다. <br><br> 덕분에 `triggerOrder==0`인 `slash` 총알과 부딪혔을 때, `slash.onTrigger`는 호출되지 않습니다. 우선순위에 밀려 무시되기 때문입니다. 그럼에도 불구하고, `ball.onTrigger`가 호출된 이후에 `slash.onTrigger`는 호출되어야 한다면, 위 예제처럼 `ball` 측에서 할일을 마친 후 직접 `slash.onTrigger`를 호출해주도록 합니다. Collision Matrix Setting을 통해서 `LayerType.PlayerAttack` 끼리는 충돌하지 않게 했으므로, 사실 의미는 없습니다. <br><br>


- `ShakeCamera()` 메소드는 카메라에 흔들림을 주는 총알을 생성합니다. `Coroutine`으로 구현할때와 다르게, 총알의 인스턴스는 계속 재사용되기 때문에 가비지 생성에 대한 부담이 줄어든다는 장점이 있습니다. 생성된 `cameraShaker` 총알은 시각적으로 보이거나 물체와 충돌하면 안되기 때문에, `cameraShaker.gameObject.layer = BulletManager.effectLayer` 처럼 이팩트용 총알로 만들어주었습니다. <br><br>

- `ScaleTime()` 메소드는 일시적인 시간정지 효과를 부여합니다. 이를 위해 `Time.timeScale`을 수정합니다. 물론 이 효과가 영구적이면 안되기에, `timeScaler`라는 총알을 생성하여 일정수만큼 프레임이 지나면 `Time.timeScale = 1f` 처럼 값을 복구하도록 지능을 주었습니다.

### 4. 마치며
튜토리얼은 여기서 마칩니다. 나머지는 `documentation.html`을 읽어보시길 바랍니다. 또한 예제에서 사용한 스프라이트(sprites)들은 `example_resources.7zip`을 다운받으시면 됩니다.

