using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public enum Type { A, B, C, D };
    public Type enemyType;
    public Transform target;
    public BoxCollider meleeArea;
    public GameObject bullet;
    public int maxHealth;
    public int curHealth;
    public bool isChase;
    public bool isAttack;
    public bool isDead;

    protected Rigidbody rigid;
    protected BoxCollider boxCollider;
    protected MeshRenderer[] meshs;
    protected NavMeshAgent nav;
    protected Animator anim;

    private void Awake() {
        rigid = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        meshs = GetComponentsInChildren<MeshRenderer>();
        nav = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();

        if (enemyType != Type.D)
            Invoke("ChaseStart", 2);
    }

    private void FixedUpdate() {
        Targeting();
        FreezeVelocity();
    }

    void FreezeVelocity() {
        if (isChase) {
            rigid.velocity = Vector3.zero;
            rigid.angularVelocity = Vector3.zero;
        }
    }

    void Targeting() {
        if (!isDead && enemyType != Type.D) {
            float targetRadius = 0;
            float targetRange = 0;

            switch (enemyType) {
                case Type.A:
                    targetRadius = 1.5f;
                    targetRange = 3f;
                    break;
                case Type.B:
                    targetRadius = 1f;
                    targetRange = 12f;
                    break;
                case Type.C:
                    targetRadius = 0.5f;
                    targetRange = 25f;
                    break;
            }

            RaycastHit[] rayHits = Physics.SphereCastAll(transform.position,
                                                         targetRadius,
                                                         transform.forward,
                                                         targetRange,
                                                         LayerMask.GetMask("Player"));

            if (rayHits.Length > 0 && !isAttack) {
                StartCoroutine(Attack());
            }
        }
    }

    IEnumerator Attack() {
        isChase = false;
        isAttack = true;
        anim.SetBool("isAttack", true);

        switch (enemyType) {
            case Type.A:
                yield return new WaitForSeconds(0.2f);
                meleeArea.enabled = true;

                yield return new WaitForSeconds(1f);
                meleeArea.enabled = false;

                yield return new WaitForSeconds(1f);
                break;
            case Type.B:
                yield return new WaitForSeconds(0.1f);
                rigid.AddForce(transform.forward * 20, ForceMode.Impulse);
                meleeArea.enabled = true;

                yield return new WaitForSeconds(0.5f);
                rigid.velocity = Vector3.zero;
                meleeArea.enabled = false;

                yield return new WaitForSeconds(2f);
                break;
            case Type.C:
                yield return new WaitForSeconds(0.5f);
                GameObject instantBullet = Instantiate(bullet, transform.position, transform.rotation);
                Rigidbody rigidBullet = instantBullet.GetComponent<Rigidbody>();
                rigidBullet.velocity = transform.forward * 20;

                yield return new WaitForSeconds(2f);
                break;
        }
        
        isChase = true;
        isAttack = false;
        anim.SetBool("isAttack", false);
    }

    void ChaseStart() {
        isChase = true;
        anim.SetBool("isWalk", true);
    }

    private void Update() {
        if (nav.enabled && enemyType != Type.D) {
            nav.SetDestination(target.position);
            nav.isStopped = !isChase;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.tag == "Melee") {
            Weapon weapon = other.gameObject.GetComponent<Weapon>();
            curHealth -= weapon.damage;
            Vector3 reactVec = transform.position - other.transform.position;

            StartCoroutine(OnDamange(reactVec, false));
        }
        else if (other.tag == "Bullet") {
            Bullet bullet = other.gameObject.GetComponent<Bullet>();
            curHealth -= bullet.damage;
            Vector3 reactVec = transform.position - other.transform.position;

            //피격 총알 제거
            Destroy(other.gameObject);

            StartCoroutine(OnDamange(reactVec, false));
        }
    }

    public void HitByGrenade(Vector3 explosionPos) {
        curHealth -= 100;
        Vector3 reactVec = transform.position - explosionPos;
        StartCoroutine(OnDamange(reactVec, true));
    }

    IEnumerator OnDamange(Vector3 reactVec, bool isGrenade) {
        foreach (MeshRenderer mesh in meshs) {
            mesh.material.color = Color.red;
        }
        yield return new WaitForSeconds(0.1f);

        if (curHealth > 0) {
            foreach (MeshRenderer mesh in meshs) {
                mesh.material.color = Color.white;
            }
        }
        else {
            foreach (MeshRenderer mesh in meshs) {
                mesh.material.color = Color.gray;
            }
            gameObject.layer = 14;
            isDead = true;
            isChase = false;
            //죽을 때 y축으로 움직이는데 nav가 방해될수 있어서 false
            nav.enabled = false;
            anim.SetTrigger("doDie");

            if (isGrenade) {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up * 3;
                rigid.freezeRotation = false;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
                rigid.AddTorque(reactVec * 15, ForceMode.Impulse);
            }
            else {
                reactVec = reactVec.normalized;
                reactVec += Vector3.up;
                rigid.AddForce(reactVec * 5, ForceMode.Impulse);
            }

            if (enemyType != Type.D)
                Destroy(gameObject, 4);
        }
    }
}
