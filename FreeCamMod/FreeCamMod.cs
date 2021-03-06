﻿using UnityEngine;
using CAMOWA;
using DIMOWAModLoader;
using System;

namespace FCM
{

    public class FreeCamMod : MonoBehaviour
    {
        private ClientDebuggerSide debugger;

        public bool isThereAPlayer = false;

        private float freeCamRotationX = 0f;
        private float freeCamRotationAround = 0f;
        private Vector3 freeCamPosition;
        
        private Transform currentCameraTransform;
        
        private Transform playerTransform;
        private PlayerLockOnTargeting playerCamController;
        private GameObject playerLockingViewOn;

        private bool rotateWithTheCamera = true;

        private float timeWhenFreezed = 0f;

        [IMOWAModInnit("Free Cam Mod", -1, 2)]
        public static void ModInnit(string porOndeTaInicializando)
        {
            Debug.Log("FreeCamMod foi iniciado em " + porOndeTaInicializando);
            new GameObject("FreeCamModCamera").AddComponent<FreeCamMod>();
            Debug.Log("FreeCamModCamera GO foi criado");
        }
        void Start()
        {
            debugger = GameObject.Find("DIMOWALevelLoaderHandler").GetComponent<ClientDebuggerSide>();

            debugger.SendLog("No start, bora-la",DebugType.LOG,LogingType.SEND_AND_LOG);
            //Debug.Log("No start, bora-lá");
            var possiblePlayerArray = (PlayerBody[])FindObjectsOfType(typeof(PlayerBody));
            if (possiblePlayerArray.Length > 0)
            {
                playerTransform = possiblePlayerArray[0].gameObject.transform;
                playerCamController = playerTransform.GetRequiredComponentInChildren<PlayerLockOnTargeting>();
                currentCameraTransform = gameObject.FindWithRequiredTag("MainCamera").transform;
                isThereAPlayer = true;
            }
            else
                currentCameraTransform = ((Camera)FindObjectOfType(typeof(Camera))).transform;
            
            playerLockingViewOn = new GameObject("FreeCamModViewLock");
            
            gameObject.AddComponent<Camera>();
            gameObject.camera.enabled = false;
            gameObject.camera.CopyFrom(currentCameraTransform.camera);

            
			playerLockingViewOn.transform.position = currentCameraTransform.position + currentCameraTransform.forward;

            freeCamPosition = Vector3.zero;
        }

        float cameraSpeed = 3f;

       

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.CapsLock) || OWInput.GetAxisPressed(PlayerCameraInput.toggleFlashlight, -1))
            {
                if (!IsFreeCamEnable())
                {
                    if (isThereAPlayer)
                    {
                        debugger.SendLog("Congelando o mov. do player", DebugType.LOG, LogingType.SEND_AND_LOG);
                        playerTransform.gameObject.GetComponent<CharacterMovementModel>().LockMovement();
                        GlobalMessenger<Camera>.FireEvent("SwitchActiveCamera", gameObject.camera);
                        playerCamController.LockOn(playerLockingViewOn.transform);
                    }
                    debugger.SendLog("Coisas Magicas", DebugType.UNKNOWN, LogingType.SEND_AND_LOG);
                    currentCameraTransform.camera.enabled = false;
                    gameObject.camera.enabled = true;
                }
                else
                {
                    if (!((SettingsMenu)FindObjectsOfType(typeof(SettingsMenu))[0]).enabled || Application.loadedLevel == 0)
                        Time.timeScale = 1f;

                    if (isThereAPlayer)
                    {
                        debugger.SendLog("Descongelando o mov. do player", DebugType.LOG, LogingType.SEND_AND_LOG);
                        playerTransform.gameObject.GetComponent<CharacterMovementModel>().UnlockMovement();
                        GlobalMessenger<Camera>.FireEvent("SwitchActiveCamera", currentCameraTransform.camera);
                        playerCamController.BreakLock();
                    }
                    debugger.SendLog("Mais coisas magicas", DebugType.UNKNOWN, LogingType.SEND_AND_LOG);
                    currentCameraTransform.camera.enabled = true;
                    gameObject.camera.enabled = false;

                    gameObject.transform.position = currentCameraTransform.position;
                    freeCamPosition = Vector3.zero;
                    freeCamRotationAround = 0f;
                    freeCamRotationX = 0f;
                }
            }
            if (IsFreeCamEnable())
            {
                if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.JoystickButton8))
                {
                    if (Time.timeScale != 0) //Se o timeScale já não é zero, fazer ele virar zero
                    {
                        Time.timeScale = 0f;
                    }//Se estamos no menu, não tem que se preocupar em voltar ao normal, mas se estamos no Solar System, vereficar se o settings menu ta aberto
                    else if (!((SettingsMenu)FindObjectsOfType(typeof(SettingsMenu))[0]).enabled || Application.loadedLevel == 0)
                        Time.timeScale = 1f;
                }
                //Cuidar da aceleração e veloc. da camera
                if (Input.GetKey(KeyCode.Tab))
                    cameraSpeed = 9f;
                else
                    cameraSpeed = 3f;
                //Direção q a camera vai virar
                if (Time.timeScale != 0)
                {
                    freeCamRotationAround += PlayerCameraInput.lookX.GetInput() * Time.deltaTime * 500f % 360;
                    freeCamRotationX -= PlayerCameraInput.lookY.GetInput() * Time.deltaTime * 500f;
                }
                else
                {
                    freeCamRotationAround += PlayerCameraInput.lookX.GetInput() * DeltaTimeWhenFreezed() * 500f % 360;
                    freeCamRotationX -= PlayerCameraInput.lookY.GetInput() * DeltaTimeWhenFreezed() * 500f;
                }
                freeCamRotationX = Mathf.Clamp(freeCamRotationX, -90f, 90f);

                if (rotateWithTheCamera)
                    gameObject.transform.rotation = currentCameraTransform.rotation;
                else
                    gameObject.transform.eulerAngles = Vector3.zero;

                gameObject.transform.rotation = gameObject.transform.rotation *  Quaternion.Euler(freeCamRotationAround * playerLockingViewOn.transform.up) * Quaternion.Euler(freeCamRotationX, 0f, 0f);
                
                //Direção q a camera vai andar
                gameObject.transform.position = currentCameraTransform.position;
                
                float variacaoDoTempo;

                if (Time.timeScale != 0)
                    variacaoDoTempo = Time.deltaTime;
                else
                {
                    variacaoDoTempo = DeltaTimeWhenFreezed();
                    if (Input.GetKey(KeyCode.W))
                        freeCamPosition += gameObject.transform.forward * cameraSpeed * variacaoDoTempo;

                    if (Input.GetKey(KeyCode.S))
                        freeCamPosition += -gameObject.transform.forward * cameraSpeed * variacaoDoTempo;

                    if (Input.GetKey(KeyCode.D))
                        freeCamPosition += gameObject.transform.right * cameraSpeed * variacaoDoTempo;

                    if (Input.GetKey(KeyCode.A))
                        freeCamPosition += -gameObject.transform.right * cameraSpeed * variacaoDoTempo;
                }
                
               
                freeCamPosition += (gameObject.transform.forward * InputChannels.moveZ.GetAxis() + gameObject.transform.right * InputChannels.moveX.GetAxis()) * 9 * variacaoDoTempo;
                

                gameObject.transform.position += freeCamPosition;
                //Fazer com q isso sempre esteja uma unidade afrente da camera principal

                playerLockingViewOn.transform.position = currentCameraTransform.position + currentCameraTransform.forward;

                //Ver qual foi o tempo desse frame
                timeWhenFreezed = Time.realtimeSinceStartup;
            }

        }
        //ZA WARUDO
        float DeltaTimeWhenFreezed()
        {
            return Time.realtimeSinceStartup - timeWhenFreezed;
        }

        bool IsFreeCamEnable()
        {
            return gameObject.camera.enabled;
        }

    } 
}

