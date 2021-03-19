using System.Collections;
using System.Collections.Generic;
using Bolt;
using UnityEngine;
using UnityEngine.Events;

//using UnityEngine.AI;


public class BaseEnemy : MonoBehaviour
{

    [Header("Enemy")]
    public GameObject player; //temp

    float maxActionTimer;
    float actionTimer;

    public float activationDistance;
    public float attackDistance = 5;


    void Start()
    {


        maxActionTimer = 0.2f;
        actionTimer = Random.Range(0, maxActionTimer);



        rb = GetComponent<Rigidbody>();
    }



    private void Update()
    {

        actionTimer -= Time.deltaTime;
        if (actionTimer <= 0)
        {
            actionTimer = maxActionTimer;
            AIUpdate();  // Move to AI manager

        }

    }


    // called every action timer based on frames in update can be set in AIupdate
    private void AIUpdate()
    {

        if (CheckDistance(player.transform.position) < activationDistance * activationDistance)
        {
            CustomEvent.Trigger(gameObject, "OnPlayerSeen", true);
            if (CheckDistance(player.transform.position) < attackDistance * attackDistance)
            {
                CustomEvent.Trigger(gameObject, "OnAttack");
            }
        }
        else
        {
            CustomEvent.Trigger(gameObject, "OnPlayerSeen", false);
        }


    }



    private void FixedUpdate()
    {
        rb.MoveRotation(Quaternion.Euler(Vector3.up * 20 * Time.deltaTime)); // set rotation
    }


    public Rigidbody rb;



    float CheckDistance(Vector3 target)
    {
        return (target - transform.position).sqrMagnitude;
    }


    private void OnDrawGizmos()
    {


        if (CheckDistance(player.transform.position) < activationDistance * activationDistance)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.white;
        }

        Gizmos.DrawWireSphere(transform.position, activationDistance);



        Gizmos.color = Color.white;

        Gizmos.DrawRay(transform.position, transform.forward * activationDistance);


    }


    void GetKnockback(float power){
    }
    void TakeDamage(){
    }    
    void DoStart(){
    }    
    void Move(){
    }
    void Attack(){
    }

    public GameObject ragdoll;
    public float knockbackMultiplier = 1;


    public float ragdollLifetime = 10;
    public bool isRagdoll = false;


    public float activateDistance;
    public float goodMoveDistance;

    public float attackTime;
    public float moveTime;

    public int damage;


    public float attackCooldown;

    protected float attackTimer;
    protected float attackCdTimer;
    protected float moveTimer;


}
