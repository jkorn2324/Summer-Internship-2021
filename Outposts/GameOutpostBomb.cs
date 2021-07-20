using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Bomb should be handled on the server.
[RequireComponent(typeof(BaseWeapon), typeof(BaseHP), typeof(Mirror.NetworkIdentity))]
public class GameOutpostBomb : Mirror.NetworkBehaviour, IChannelTarget
{
    #region enums_delegates
    public enum BombState
    {
        STATE_INACTIVE,
        STATE_CHANNELED,
        STATE_SHIELDED,
        STATE_PRIMED
    }

    public delegate void GameOutpostBombSignature(GameOutpostBomb bomb);
    public GameOutpostBombSignature BombChannelDetonationFinishedDelegate;
    public GameOutpostBombSignature BombDetonatedEventDelegate;

    #endregion

    #region fields

    [Header("Bomb References")]

    [SerializeField]
    private GameOutpost outpost;
    [SerializeField]
    private BaseScripts.BaseInteractable interactable;

    [Header("Bomb Settings")]

    [SerializeField, Min(0)]
    private float bombChannelTimer;
    [SerializeField, Min(0)]
    private float bombPrimedTimer;
    [SerializeField]
    private LayerMask blastLayerMask;
    [SerializeField, Min(0)]
    private float blastRadius;

    [Header("Audio")]

    [SerializeField, Min(0)]
    private float minBombSoundTimer;
    [SerializeField, Min(1)]
    private float maxBombSoundTimer;
    [SerializeField]
    private AudioClip bombPrimedSoundTimer;

    [Header("Visuals - Beam")]

    [SerializeField]
    private GameObject beam;
    [SerializeField, Range(0.1f, 1.0f)]
    private float beamStartInterpolatedY;

    [Header("Visuals - Emission")]

    [SerializeField]
    private string shaderEmissionVariable;
    [SerializeField, Min(0)]
    private float minEmission;
    [SerializeField]
    private List<MeshRenderer> emissionMeshRenderers;

    [Header("Visuals - Color")]

    [SerializeField]
    private bool isShaderVariableTexture = false;
    [SerializeField]
    private string shaderColorVariable;
    [SerializeField]
    private Color defaultBeamColor;
    [SerializeField]
    private Color channeledBeamColor;
    [SerializeField]
    private Color primedBeamColor;
    [SerializeField, Range(0.0f, 2.0f)]
    private float interpolationColorTime;


    private Color _prevColorBeam;
    private Color _currentColorBeam;
    private Color _currentColor;

    private BaseHP _baseHealthComponent;
    private BaseDestroyHPListener _hpDestroyListener;

    private bool _detonated = false;
    [Mirror.SyncVar(hook = nameof(HandleBombStateChanged))]
    private BombState _bombState = BombState.STATE_INACTIVE;

    private Vector3 _originalBeamPosition;
    private Vector3 _calculatedBeamPosition;

    private List<float> _emissionAmounts = new List<float>();

    private float _currentColorInterpolatedTime = 0.0f;
    private float _currentPrimedSeconds = 0.0f;
    private float _maxPrimedRepeatingSoundTimer = 0.0f;
    private float _primedRepeatingSoundTimer = 0.0f;

    private float _channelTime = 0.0f;

    #endregion

    #region properties

    public BombState State => _bombState;
    public GameOutpost Outpost => outpost;

    public float TimeLeftUntilChannelFinish
        => _channelTime;

    public float ChannelProgressPercentage
    {
        get
        {
            return Mathf.Clamp01((bombChannelTimer - _channelTime) / bombChannelTimer) * 100.0f;
        }
    }

    #endregion

    private void Start()
    {
        _prevColorBeam = _currentColorBeam = defaultBeamColor;
        _channelTime = bombChannelTimer;
        _hpDestroyListener = GetComponent<BaseDestroyHPListener>();
        _baseHealthComponent = GetComponent<BaseHP>();
        InitializeBeamPositions();

        // Allocates the emission for each mesh renderer for the bomb.
        foreach(MeshRenderer renderer in emissionMeshRenderers)
        {
            if(renderer)
            {
                Material material = renderer.material;
                if (material)
                {
                    _emissionAmounts.Add(
                        material.GetFloat(shaderEmissionVariable));
                }
            }
        }

        if(HRNetworkManager.Get
            && HRNetworkManager.Get.bIsServer)
        {
            _baseHealthComponent.SetInvincible(true);
        }

        HookEvents();
    }

    private void InitializeBeamPositions()
    {
        if (beam)
        {
            Vector3 currentPosition = beam.transform.position;
            Vector3 localScale = beam.transform.localScale;
            float endYOffset = localScale.y + currentPosition.y;
            float startYOffset = beamStartInterpolatedY * endYOffset;
            _originalBeamPosition = currentPosition;
            _originalBeamPosition.y -= startYOffset;
            _calculatedBeamPosition = beam.transform.position;
            _calculatedBeamPosition.y -= endYOffset;
        }
    }

    private void OnEnable()
    {
        HookEvents();
    }

    private void OnDisable()
    {
        UnHookEvents();
    }

    private void HookEvents() { }

    private void UnHookEvents() { }

    private void Update()
    {
        switch (_bombState)
        {
            case BombState.STATE_CHANNELED:
                UpdateChannel(Time.deltaTime);
                break;
            case BombState.STATE_INACTIVE:
                break;
            case BombState.STATE_PRIMED:
                UpdatePrimed(Time.deltaTime);
                break;
            case BombState.STATE_SHIELDED:
                break;
        }
        UpdateColor(Time.deltaTime);
    }

    private void SetState(BombState bombState)
    {
        if (HRNetworkManager.Get
            && HRNetworkManager.Get.bIsServer)
        {
            HandleBombStateChanged(_bombState, bombState);
        }
        else
        {
            NetworkSetBombState_Server(bombState);
        }
    }

    [Mirror.Command(ignoreAuthority = true)]
    private void NetworkSetBombState_Server(BombState state)
    {
        _bombState = state;
    }

    private void HandleBombStateChanged(BombState prevState, BombState newState)
    {
        if(newState == BombState.STATE_PRIMED)
        {
            _maxPrimedRepeatingSoundTimer = _currentPrimedSeconds = bombPrimedTimer;
        }
        else if (newState == BombState.STATE_CHANNELED
            || prevState == BombState.STATE_CHANNELED)
        {
            _channelTime = bombChannelTimer;
        }

        _currentColorInterpolatedTime = interpolationColorTime;
        _prevColorBeam = GetColorFromState(prevState);
        _currentColorBeam = GetColorFromState(newState);

        _bombState = newState;
    }

    private Color GetColorFromState(BombState state)
    {
        switch (state)
        {
            case BombState.STATE_CHANNELED:
                return channeledBeamColor;
            case BombState.STATE_PRIMED:
                return primedBeamColor;
        }
        return defaultBeamColor;
    }

    private void UpdateChannel(float deltaTime)
    {
        _channelTime -= deltaTime;

        if(_channelTime <= 0.0f)
        {
            HandleChannelFinished();
        }
    }

    private void UpdatePrimed(float deltaTime)
    {
        if (!_detonated)
        {
            _currentPrimedSeconds -= deltaTime;

            if (_currentPrimedSeconds <= 0.0f)
            {
                HandleBombDetonated();
            }

            // Updates audio and visuals for the bomb.
            UpdatePrimedShaders();

            float bombTimeCompletionPercentage =
                (bombPrimedTimer - _currentPrimedSeconds) / bombPrimedTimer;
            if (beam)
            {
                beam.transform.position = Vector3.Lerp(
                    _originalBeamPosition, _calculatedBeamPosition, bombTimeCompletionPercentage);
            }

            _primedRepeatingSoundTimer -= deltaTime;

            if (_primedRepeatingSoundTimer <= 0.0f)
            {
                _maxPrimedRepeatingSoundTimer =
                    _primedRepeatingSoundTimer = Mathf.Lerp(maxBombSoundTimer,
                        minBombSoundTimer, bombTimeCompletionPercentage);

                PlayPrimedTimerSound();
            }
        }
    }

    private void UpdateColor(float deltaTime)
    {
        if(_currentColorInterpolatedTime > 0.0f)
        {
            _currentColorInterpolatedTime -= deltaTime;

            if(_currentColorInterpolatedTime <= 0.0f)
            {
                _currentColorInterpolatedTime = 0.0f;
            }

            float calculatedInterpolatedTime = (interpolationColorTime - _currentColorInterpolatedTime) / interpolationColorTime;
            Color calculatedColor = Color.Lerp(
                _prevColorBeam, _currentColorBeam, calculatedInterpolatedTime);
            SetColor(calculatedColor);
        }
    }

    private void SetColor(Color color)
    {
        if(_currentColor == color)
        {
            return;
        }

        _currentColor = color;

        for(int i = 0; i < emissionMeshRenderers.Count; i++)
        {
            MeshRenderer renderer = emissionMeshRenderers[i];
            if(renderer)
            {
                Material material = renderer.material;
                if(material)
                {
                    if(isShaderVariableTexture)
                    {
                        // TODO: Fix lag here...
                        Texture2D texture = material.GetTexture(shaderColorVariable) as Texture2D;
                        Texture2D newTexture = new Texture2D(texture.width, texture.height); 
                        Color[] textureColors = new Color[texture.width * texture.height];
                        for(int px = 0; px < textureColors.Length; px++)
                        {
                            textureColors[px] = color;
                        }
                        newTexture.SetPixels(textureColors);
                        newTexture.Apply();
                        material.SetTexture(shaderColorVariable, newTexture);
                    }
                    else
                    {
                        material.SetColor(shaderColorVariable, color);
                    }
                }
            }
        }
    }

    private void UpdatePrimedShaders()
    {
        float emissionInterpolated = (_maxPrimedRepeatingSoundTimer - _primedRepeatingSoundTimer)
            / _maxPrimedRepeatingSoundTimer;
        float graphedEmission = Mathf.Clamp01(
            Mathf.Cos(emissionInterpolated * 2.0f * Mathf.PI) * 0.5f + 0.5f);

        for(int i = 0; i < emissionMeshRenderers.Count; i++)
        {
            MeshRenderer beamMeshRenderer = emissionMeshRenderers[i];
            if(beamMeshRenderer)
            {
                Material material = beamMeshRenderer.material;
                if(material)
                {
                    float interpolatedEmission = Mathf.Lerp(
                        _emissionAmounts[i], minEmission, graphedEmission);
                    material.SetFloat(shaderEmissionVariable, interpolatedEmission);
                }
            }
        }
    }

    private void PlayPrimedTimerSound()
    {
        float defaultVolume = 1.0f;
        HeroPlayerCharacter clientPlayer = ((HRGameInstance)BaseGameInstance.Get)?.GetFirstPawn() as HeroPlayerCharacter;
        if (clientPlayer)
        {
            float distance = Vector3.Distance(
                clientPlayer.transform.position, this.transform.position);
            defaultVolume = Mathf.Max(0.0f,
                (blastRadius - distance) / blastRadius);
        }
        DynamicAudioPlayer.PlayDynamicSound(bombPrimedSoundTimer, defaultVolume);
    }

    private void HandleBombDetonated()
    {
        _detonated = true;
        BombDetonatedEventDelegate?.Invoke(this);

        if(HRNetworkManager.Get
            && HRNetworkManager.Get.bIsServer)
        {
            DestroyObjectsWithinBlastRadius();
        }

        if (_hpDestroyListener)
        {
            _hpDestroyListener.DestroyObject(null);
        }
    }

    private void DestroyObjectsWithinBlastRadius()
    {
        Collider[] objectsWithinBlastRadius = Physics.OverlapSphere(
            this.transform.position, blastRadius, blastLayerMask);
        
        foreach(Collider collider in objectsWithinBlastRadius)
        {
            BaseHP healthComponent = collider?.GetComponentInParent<BaseHP>();
            if(healthComponent)
            {
                healthComponent.SetHP_IgnoreAuthority(0.0f, this.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(this.transform.position, 0.5f);

        Handles.color = Color.red;
        Handles.DrawWireDisc(this.transform.position, Vector3.up, blastRadius);
    }

    private void HandleChannelFinished()
    {
        SetState(BombState.STATE_PRIMED);
        BombChannelDetonationFinishedDelegate?.Invoke(this);
    }

    public void HandleBeginChannel(AHeroChannel channel)
    {
        if(channel is HeroBombChannel)
        {
            SetState(BombState.STATE_CHANNELED);
        }
    }

    public void HandleEndChannel(AHeroChannel channel, ChannelRemovalData removalData)
    {
        if(channel is HeroBombChannel
            && _bombState == BombState.STATE_CHANNELED)
        {
            SetState(BombState.STATE_INACTIVE);
        }
    }
}
