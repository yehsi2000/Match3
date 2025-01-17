using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour {
    [SerializeField]
    GameObject popParticle;

    [SerializeField]
    GameObject[] specialParticles;

    List<ParticleSystem> particlePool;
    List<List<ParticleSystem>> specialParticlePool;

    private void Awake() {
    }

    // Start is called before the first frame update
    void Start() {
        particlePool = new List<ParticleSystem>();
        specialParticlePool = new List<List<ParticleSystem>>();

        //fill the pool
        for (int i = 0; i <= System.Enum.GetValues(typeof(SpecialType.ESpecialType)).Length; ++i) {
            specialParticlePool.Add(new List<ParticleSystem>());
        }
    }

    public void KillParticle(Board board, Vector2 pointPos, INodeType val) {
        List<ParticleSystem> available = new List<ParticleSystem>();
        //special effect
        if (val is SpecialType) {
            var specialval = val as SpecialType;
            for (int i = 0; i < specialParticlePool[(int)specialval.TypeVal].Count; i++) {
                if (specialParticlePool[(int)specialval.TypeVal][i].isStopped) {
                    available.Add(specialParticlePool[(int)specialval.TypeVal][i]);
                }
            }
        }
        else {
            //no special effect
            for (int i = 0; i < particlePool.Count; i++) {
                if (particlePool[i].isStopped) {
                    available.Add(particlePool[i]);
                }
            }
        }

        ParticleSystem particle = null;

        if (available.Count > 0) {
            particle = available[0];
        }
        else {
            GameObject particleEffect;

            if (val is SpecialType) {
                particleEffect = specialParticles[(int)(val as SpecialType).TypeVal];
            }
            else {
                particleEffect = popParticle;
            }

            GameObject particleObject = GameObject.Instantiate(particleEffect, board.KilledBoard.transform);
            ParticleSystem objParticle = particleObject.GetComponent<ParticleSystem>();
            particle = objParticle;

            if (val is SpecialType) {
                specialParticlePool[(int)(val as SpecialType).TypeVal].Add(objParticle);
            }
            else {
                particlePool.Add(objParticle);
            }
        }
        particle.transform.position = pointPos;
        particle.Play();
    }
}