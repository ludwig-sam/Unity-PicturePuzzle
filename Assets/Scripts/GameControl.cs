using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//控制游戏的整体
public class GameControl : MonoBehaviour {

    //游戏状态
    enum STEP
    {
        NONE=-1,

        PLAY =0,    //游戏中
        CLEAR,      //清空

        NUM,
    };

    private STEP step = STEP.PLAY;
    private STEP next_step = STEP.NONE;

    private float step_timer = 0.0f;

    //----------------------------------------------------------//
    //拼图预制体
    public GameObject PazzlePrefab = null;
    //拼图身上的脚本
    private PazzleControl pazzle_control = null;
    //
    private Texture finish_texture = null;

    //-----------------------------------------------------------//

        public enum SE
    {
        NONE = -1,

        GRAB = 0,       // 点击拖拽碎片时
        RELEASE,        // 松开碎片时（非正解的情况下）

        ATTACH,         // 松开碎片时（正解的情况下）

        COMPLETE,       // 拼图完成时的音乐

        BUTTON,         // GUI 按钮

        NUM,
    };

    //-----------------------------------------------------------//

    //音频数组，来控制音频的播放
    public AudioClip[] audio_clips;

	// Use this for initialization
	void Start () {
        //实例化出拼图，并获取到拼图身上的控制脚本
        this.pazzle_control = (Instantiate(this.PazzlePrefab) as GameObject).GetComponent<PazzleControl>();

        
    }

    //播放声音
    public void PlaySe(SE se)
    {
        this.GetComponent<AudioSource>().PlayOneShot(this.audio_clips[(int)se]);
    }

    //按下“重新开始”按钮时的处理
    public void OnRetryButtonPush()
    {
        //if (!this.pazzle_control.isCleared())
        //{
            //播放印象
            this.PlaySe(GameControl.SE.BUTTON);
            //调用重新开始方法
            this.pazzle_control.BeginRetryAction();
        //}
    }
}
