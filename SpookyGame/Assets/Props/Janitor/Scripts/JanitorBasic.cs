using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;

public class JanitorBasic : MonoBehaviour
{
    // Start is called before the first frame update
    

    NavMeshAgent agent;
    public Transform playerpos;
    public bool Wandering;
    public bool Chasing;
    public float detection=0;
    JanitorFOV fov;
    public float detectionTime;
    public float ExtraChaseTime;
    Vector3 hidingplace;
    bool inRange;
    PlayerController player;
    bool knowshider;
    private IEnumerator Chasecoroutine,Wandercoroutine;
    [SerializeField] Transform[] PatrolPoints;
    Animator animator;
    float OGplayerspeed;
    [SerializeField] float BasicSpeed;
    [SerializeField] Transform freezerpos;
    [SerializeField] Transform punchposition;
    [SerializeField] float maxDetection;
    public GameObject punchtrigger;
    float cooldown = 1;

    bool SecondCatch;
    public Camera grabcamera;
     Camera maincamera;
    public GameObject BlackOutScreen,punchblackout;
    public GameObject LoseScreen;
    
    public PostProcessVolume hurteffect;
    IEnumerator grabcoroutine;

    float contactTime;

    bool inCutscene;
    public AudioSource detectionSound;
    public AudioSource chasesong;

    void Awake()
    {
        maincamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<PlayerController>();

        OGplayerspeed = player.speed;
      playerpos = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        fov = GetComponent<JanitorFOV>();
    }
    void Start()
    {
        
    }

    public void PlayerHurt()
    {
        if(hurteffect.weight == 0)
        {
            
            cooldown = 3f;
            Physics.IgnoreCollision(GetComponent<BoxCollider>(), GameObject.FindGameObjectWithTag("Player").GetComponent<CapsuleCollider>(), true);

            player.enabled = false;
            playerpos.GetComponent<Rigidbody>().AddForce(transform.forward * 17,ForceMode.Impulse);
            hurteffect.weight = 1;
            IEnumerator reducehurtcoroutine;
            reducehurtcoroutine = ReduceHurtNumerator();
            StartCoroutine(reducehurtcoroutine);
        }
        else
        {
            agent.speed = BasicSpeed * 4;
            Instantiate(punchblackout, transform.position, Quaternion.identity);
            grabcoroutine = GrabCutscene();
            StartCoroutine(grabcoroutine);
        }

    }
    IEnumerator ReduceHurtNumerator()
    {
        yield return new WaitForSeconds(1);
        player.enabled = true;

        while (hurteffect.weight > 0)
        {
            hurteffect.weight -= Time.deltaTime * .25f;
            yield return new WaitForSeconds(.01f);
        }
        hurteffect.weight = 0;
        yield return null;

    }

    IEnumerator GrabCutscene()
    {

        player.speed = 0;
        inCutscene = true;
        StopCoroutine(Chasecoroutine);
        detection = 0;
        agent.speed = BasicSpeed * 2;

        Vector3 dir = playerpos.position - transform.position;
        Quaternion rot = Quaternion.LookRotation(dir);
        for (int i = 0; i < 30; i++)
        {
            yield return null;
            dir = playerpos.position - transform.position;
            dir.y = 0;//This allows the object to only rotate on its y axis
            rot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Lerp(transform.rotation, rot, 10 * Time.deltaTime);
           
        }
        while (chasesong.volume > 0)
        {
            yield return null;
            chasesong.volume -= Time.deltaTime;
        }

        if(!player.hiddendesk)
        {
            if(player.currentlocker)
            {
                player.currentlocker.Interaction();

            }
            animator.SetBool("grablock", true);

        }
        else
        {
            animator.SetBool("grabunder", true);

        }

        yield return new WaitForSeconds(3.1f);
        if (!SecondCatch)
        {

            while (agent.remainingDistance > .1f)
            {
                
                yield return null;
            }
            agent.speed = BasicSpeed;
            SecondCatch = true;

            if (!player.hiddendesk)
            {
                animator.SetBool("grablock", false);

            }
            else
            {
                animator.SetBool("grabunder", false);

            }

            yield return new WaitForSeconds(5);
            transform.position = PatrolPoints[Random.Range(2, 4)].position;
            agent.SetDestination(PatrolPoints[Random.Range(0, PatrolPoints.Length)].position);
            Chasing = false;
            inRange = false;
            inCutscene = false;

            yield return new WaitForSeconds(1);
            player.gameObject.SetActive(true);

            player.is_crouched = false;
            player.transform.localScale = new Vector3(1, 1, 1);
            player.is_hidden = false;

            playerpos.position = freezerpos.position;
            grabcamera.tag = "Untagged";
            grabcamera.gameObject.SetActive(false);
            maincamera.tag = "MainCamera";
            player.speed = OGplayerspeed;
         


        }
        else
        {
            yield return new WaitForSeconds(4);
            GameObject screen = Instantiate(LoseScreen, transform.position, Quaternion.identity);
            screen.GetComponentInChildren<Text>().text = "The Janitor Caught You";



        }

    }



    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            inRange = true;
          if(!player.is_hidden && !inCutscene && cooldown <= 0)
            {
                cooldown = 1;
                for(int i = 0;i<20;i++)
                {

                    Vector3 dir = playerpos.position - transform.position;
                    dir.y = 0;//This allows the object to only rotate on its y axis
                    Quaternion rot = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Lerp(transform.rotation, rot, 10 * Time.deltaTime);
                    if(transform.rotation == rot)
                    {
                        i = 21;
                    }
                    
                }
                animator.SetTrigger("punch");
                detection += detectionTime;

            }
          
        }
    }



   public void Punch()
    {
       
        Instantiate(punchtrigger, punchposition.position, Quaternion.identity);
    }


    private void OnTriggerExit(Collider other)
    {
        animator.ResetTrigger("punch");
        if (other.CompareTag("Player"))
        {
            inRange = false;

        }
    }

    public void PlayerHide(Vector3 pos)
    {
        hidingplace = pos;
      
       
        if (fov.canSeePlayer || inRange || contactTime >= 0)
        {
            knowshider = true;

        }
        else
        {

            knowshider = false;
        }

        if (Chasing)
        {
            detection += 7;
        }
    }

    IEnumerator WanderingNumerator()
    {
        animator.SetFloat("walkspeed", 1);
        agent.speed = BasicSpeed;
        while (Wandering)
        {
            Vector3 randompatrol = PatrolPoints[Random.Range(0, PatrolPoints.Length)].position;
            agent.SetDestination(randompatrol);
            yield return new WaitForSeconds(.3f);
            if (agent.remainingDistance < 2f)
            {
                 randompatrol = PatrolPoints[Random.Range(0, PatrolPoints.Length)].position;
                agent.SetDestination(randompatrol);
            }
            yield return new WaitForSeconds(.3f);

            while (agent.remainingDistance > .1f)
            {
                
                yield return null;
            }
           
            animator.SetBool("sad", true);
            int rand = Random.Range(0, 6);
            yield return new WaitForSeconds(3 + rand);
            animator.SetBool("sad", false);
           
        }

    }

    public void JanitorGrabed()
    {
        maincamera.tag = "Untagged";
        grabcamera.gameObject.SetActive(true);
        grabcamera.tag = "MainCamera";
        player.gameObject.SetActive(false);
        if(!player.hiddendesk)
        {
            grabcamera.GetComponent<Animator>().SetTrigger("locker");
        }
      

    }

    public void JanitorFreezeThrow()
    {
    
            grabcamera.GetComponent<Animator>().SetTrigger("throw");
        
  
        Instantiate(BlackOutScreen, transform.position, Quaternion.identity);
    }

    public void JanitorGrabFreezer()
    {
        if(SecondCatch)
        {
            grabcamera.GetComponent<Animator>().SetTrigger("kill");
            
        }
        else
        {
           
            agent.speed = BasicSpeed * 2;
            agent.SetDestination(freezerpos.position);
        }
      
    }


    IEnumerator ChaseNumerator()
    {
        detectionSound.Stop();
        chasesong.Play();
        chasesong.volume = 1;
        detectionSound.volume = 0;

      

        detection += ExtraChaseTime;
        animator.SetBool("sad", false);
        if(Wandercoroutine != null)
        {
            StopCoroutine(Wandercoroutine);
        }


        knowshider = false;
        Wandering = false;

        while (detection > 0)
        {
            yield return null;
          if(!player.is_hidden)
            {
                agent.SetDestination(playerpos.position);
                if(inRange)
                {
                    agent.speed = 1f;

                }
                else
                {
                    animator.SetFloat("walkspeed", 2);
                    agent.speed = BasicSpeed * 2;
                }


            }
          else
            {
                agent.SetDestination(hidingplace);
              
                if(agent.remainingDistance < .1f)
                {

                    detection = 0;
                    if(knowshider)
                    {
                        grabcoroutine = GrabCutscene();
                        StartCoroutine(grabcoroutine);


                        yield return new WaitForSeconds(.1f);
                      while(inCutscene)
                        {
                            yield return null;
                        }

                    }
                    else
                    {
                        animator.SetTrigger("confused");
                    }

                    yield return new WaitForSeconds(4);

                }
                   
            }

          


        }
        animator.SetFloat("walkspeed", 1);

        Chasing = false;
        while(chasesong.volume > 0)
        {
            yield return null;
            chasesong.volume -= Time.deltaTime;
        }
      
    }



    // Update is called once per frame
    void Update()
    {
   
        if(cooldown > 0)
        {
            cooldown -= Time.deltaTime;
        }
        else
        {
            if (GameObject.FindGameObjectWithTag("Player") )
            {
                Physics.IgnoreCollision(GetComponent<BoxCollider>(), GameObject.FindGameObjectWithTag("Player").GetComponent<CapsuleCollider>(), false);

            }

            cooldown = 0;
        }

        if(detection > maxDetection)
        {
            detection = maxDetection;
        }

      if(fov.canSeePlayer)
       {
            detection += Time.deltaTime ;
            detection += .05f - Mathf.Clamp(Vector3.Distance(playerpos.position, transform.position),0,4) * .01f;

            contactTime += Time.deltaTime;
            if(contactTime > 2)
            {
                contactTime = 2;
            }
            
            

            if(!Chasing)
            {
                if(!detectionSound.isPlaying)
                {
                    detectionSound.Play();

                }
                detectionSound.volume += Time.deltaTime * .5f; 
            }

            if(detection >= detectionTime && !Chasing && !inCutscene)
            {
                Chasecoroutine = ChaseNumerator();
                StartCoroutine(Chasecoroutine);
                Chasing = true;
            }
        }
        else
        {
            if (contactTime > 0)
            {
                contactTime -= Time.deltaTime;
            }
          
           

            if (detection > 0 )
            {
                detectionSound.volume -= Time.deltaTime * .5f;
                detection -= Time.deltaTime;
            }

        }
        
        if(detection <= 0 && !Wandering && !Chasing && !inCutscene)
        {
           
            Wandering = true;
            Wandercoroutine = WanderingNumerator();
            StartCoroutine(Wandercoroutine);

        }

        
    }
}
