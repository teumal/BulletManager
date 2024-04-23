# BulletManager

## Overview
2D 탑뷰 시점 슈팅 게임에서 총알을 관리하기 위한 script component 입니다. `Bullet`, `BulletManager`, `MobBase` 이렇게 세 개의 C# script 가 존재합니다. <br><br>

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

총알에 지능을 주지 않으면, 처음에 주어진 위치(position)에서 정해진 방향(lookAt)과 속도(speed)로 날라가다가 화면 밖으로 나갈 경우, 파괴되는 지능이 없는 총알을 생성합니다. 

<br><br>

## 3. 총알의 지능
총알은 단순한 발사체(projectile)가 아닙니다. `onUpdate`, `onTrigger`, `onCollision`, `onDestroy` 등의 총 4가지의 지능을 통해, 적을 추적하거나 총알이 총알을 소환하는 마치 적(enemy)으로서도 행동할 수 있으며, 레이저가 되거나, 서로의 총알을 튕겨내는 등 여러가지의 것들을 할 수 있습니다. 


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

위 코드에서 레이저의 기둥을 표현하기 위해서 `LineRenderer`를 사용했습니다. `lineRenderer.sharedMaterial` 을 사용했는데, 이는 기존의 `lineRenderer.material`이 항상 새로운 `Material` 객체를 만들어내기 때문입니다. Render Pipeline 에 따라서 `MaterialPropertyBlock` 을 고려해볼 수 있겠습니다.  <br><br>




