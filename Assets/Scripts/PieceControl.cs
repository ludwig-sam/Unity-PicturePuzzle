using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//I

//MeshCollider是必需的
[RequireComponent(typeof(MeshCollider))]
public class PieceControl : MonoBehaviour
{
    //游戏摄像机
    private GameObject game_camera;

    public PazzleControl pazzle_control = null;

    public GameControl game_control = null;

    //--------------------------------------------------//

    //拖动时，鼠标光标通常位于最初点击处并随碎片一起移动
    //false时，鼠标光标位置=碎片中心
    private static bool IS_ENABLE_GRAB_OFFSET = false;

    private static float HEIGHT_OFFSET_BASE = 0.1f;

    private static float SNAP_SPEED_MIN = 0.01f * 60.0f;
    private static float SNAP_SPEED_MAX = 0.8f * 60.0f;

    //---------------------------------------------------//

    //鼠标拖拽的位置与碎片中心的插值
    private Vector3 grab_offset = Vector3.zero;

    //是否拖拽中？
    private bool is_now_dragging = false;

    //
    public Vector3 finished_position;

    //开始的位置
    public Vector3 start_position;

    public float height_offset = 0.0f;

    public float roll = 0.0f;

    //吸附距离
    //当距离正确位置够近时松开按键，碎片被吸附到正确的位置
    static float SNAP_DISTANCE = 0.5f;

    enum STEP
    {
        NONE = -1,

        IDLE = 0,       //没有得到正解
        DRAGING,        //拖动中
        FINISHED,       //放置到了正确的位置
        RESTART,        //重新开始
        SNAPPING,       //吸附过程中

        NUM,
    };

    //碎片当前的状态
    private STEP step = STEP.NONE;

    private STEP next_step = STEP.NONE;

    //吸附时的目标位置
    private Vector3 snap_target;

    //吸附后的移动状态
    private STEP next_step_snap;

    //----------------------------------------------------//

    void Awake()
    {
        //记录下初始位置（最后的正确位置
        this.finished_position = this.transform.position;

        //初始化，后续将使用移动后的位置来替换
        this.start_position = this.finished_position;
    }

    // Use this for initialization
    void Start()
    {
        //查找摄像机的实例对象
        this.game_camera = GameObject.FindGameObjectWithTag("MainCamera");

        this.game_control = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameControl>();
    }

    // Update is called once per frame
    void Update()
    {
        //当拾取的碎片移动到正解位置附近时，碎片变成此颜色
        Color color = Color.white;

        #region //状态迁移
        switch (this.step)
        {
            case STEP.NONE:
                {
                    this.next_step = STEP.RESTART;
                }
                break;
            case STEP.IDLE://没有得到正解
                {
                    if (this.is_now_dragging)//允许拖动
                    {
                        //开始拖动
                        this.next_step = STEP.DRAGING;
                    }
                }
                break;
            case STEP.DRAGING://拖动中
                {
                    if (this.Is_In_Snap_Range())
                    {
                        //允许吸附时

                        bool is_snap = false;

                        //松开鼠标按钮时开始吸附
                        if (!this.is_now_dragging)
                        {
                            is_snap = true;
                        }

                        if (is_snap)
                        {
                            //这个碎片的处理已经结束
                            this.next_step = STEP.SNAPPING;
                            this.snap_target = this.finished_position;
                            this.next_step_snap = STEP.FINISHED;

                            this.game_control.PlaySe(GameControl.SE.ATTACH);
                        }
                    }
                    else
                    {
                        //处于无法吸附的区域（离正解位置较远）时

                        if (!this.is_now_dragging)//松开按键
                        {

                            this.next_step = STEP.IDLE;

                            //播放音乐
                            //togo
                            this.game_control.PlaySe(GameControl.SE.RELEASE);
                        }
                    }
                }
                break;
            case STEP.SNAPPING:
                {
                    //进入到目标位置后结束
                    if ((this.transform.position - this.snap_target).magnitude < 0.0001f)
                    {
                        this.next_step = this.next_step_snap;
                    }
                }
                break;
        }

        #endregion


        #region//状态迁移时的初始化处理
        while (this.next_step != STEP.NONE)
        {
            this.step = this.next_step;

            this.next_step = STEP.NONE;

            switch (this.step)
            {
                
                case STEP.IDLE:
                    {
                        this.SetHeightOffset(this.height_offset);
                    }
                    break;
                case STEP.DRAGING:
                    {
                        this.Begin_Dragging();

                        //将拖动开始事件通知给拼图管理类
                        this.pazzle_control.PickPiece(this);

                        //播放声音togo
                        this.game_control.PlaySe(GameControl.SE.GRAB);

                    }
                    break;
                case STEP.FINISHED:
                    {
                        //吸附到正确位置
                        this.transform.position = this.finished_position;

                        //通知拼图管理类将这个碎片放置到正确的位置
                        this.pazzle_control.FinishPiece(this);
                    }
                    break;
                case STEP.RESTART:
                    {
                        this.transform.position = this.start_position;

                        this.SetHeightOffset(this.height_offset);

                        this.next_step = STEP.IDLE;
                    }
                    break;
            }
        }
        #endregion


        #region//各个状态的执行
        this.transform.localScale = Vector3.one;

        switch (this.step)
        {
            case STEP.DRAGING:
                {
                    this.Do_dragging();

                    //进入允许吸附的范围（非常靠近正解位置时）后，颜色变亮
                    if (this.Is_In_Snap_Range())
                    {
                        color *= 1.5f;
                    }
                    this.transform.localScale = Vector3.one * 1.1f;
                }
                break;
            case STEP.SNAPPING:
                {
                    //朝目标位置移动
                    Vector3 next_position, distance, move;

                    //
                    distance = this.snap_target - this.transform.position;

                    //下一处位置=当前位置和目标位置的中心点
                    distance *= 0.25f*(60.0f*Time.deltaTime);

                    next_position = this.transform.position + distance;

                    move = next_position - this.transform.position + distance;

                    float snap_speed_min = PieceControl.SNAP_SPEED_MIN * Time.deltaTime;
                    float snap_speed_max = PieceControl.SNAP_SPEED_MAX * Time.deltaTime;

                    if (move.magnitude < snap_speed_min)
                    {
                        //
                        this.transform.position = this.snap_target;
                    }
                    else
                    {
                        //如果移动速度过快则调整
                        if (move.magnitude > snap_speed_max)
                        {
                            move *= snap_speed_max / move.magnitude;

                            next_position = this.transform.position + move;
                        }
                        this.transform.position = next_position;
                    }
                }
                break;
        }

        #endregion

        this.GetComponent<Renderer>().material.color = color;

    }

    //拖动开始时的处理
    private void Begin_Dragging()
    {
        do
        {
            //将光标坐标变换为3D空间内的世界坐标

            Vector3 world_position;
            if (!this.Unproject_mouce_position(out world_position,Input.mousePosition))
            {
                break;
            }
            if (PieceControl.IS_ENABLE_GRAB_OFFSET)
            {

                //求出偏移值
                this.grab_offset = this.transform.position - world_position;
            }
        } while (false);
    }

    //拖动中的处理,使鼠标在碎片中心
    private void Do_dragging()
    {
        do
        {
            //将光标坐标变换为3D空间内的世界坐标
            Vector3 world_position;

            if (!this.Unproject_mouce_position(out world_position,Input.mousePosition))
            {
                break;
            }

            //加上光标坐标（3D)的偏移值，计算出碎片的中心坐标
            this.transform.position = world_position + this.grab_offset;
        } while (false);
    }

    //获取碎片的边框矩形
    public Bounds GetBounds(Vector3 center)
    {
        Bounds bounds = this.GetComponent<MeshFilter>().mesh.bounds;

        //设置中心位置
        //
        bounds.center = center;

        return (bounds); 

    }

    public void Restart()
    {
        this.next_step = STEP.RESTART;

    }
    //按下鼠标按键时
    void OnMouseDown()
    {
        this.is_now_dragging = true;
    }

    //松开鼠标按键时
     void OnMouseUp()
    {
        this.is_now_dragging = false;
    }

    //设置高度的偏移
    public void SetHeightOffset(float _height_offset)
    {
        Vector3 position = this.transform.position;

        this.height_offset = 0.0f;

        //只有放置到正解位置前有效
        if (this.step != STEP.FINISHED || this.next_step!=STEP.FINISHED)
        {
            this.height_offset = _height_offset;

            position.y = this.finished_position.y + PieceControl.HEIGHT_OFFSET_BASE;
            position.y += this.height_offset;

            this.transform.position = position;
        }
    }

    //将鼠标的位置，变换为3D空间内的世界坐标
    //·穿过鼠标光标和摄像机位置的直线
    //·用于判定是否和地面碰撞的平面
    //求出二者的交点
    public bool Unproject_mouce_position(out Vector3 world_position,Vector3 mouse_position)
    {
        bool ret;
        float depth;

        //通过碎片的中心的水平（法线为Y轴，zx平面）面
        Plane plane = new Plane(Vector3.up, new Vector3(0.0f, this.transform.position.y, 0.0f));

        //穿过摄像机位置和鼠标光标位置的直线
        Ray ray = this.game_camera.GetComponent<Camera>().ScreenPointToRay(mouse_position);

        //求出上面二者的交点
        if (plane.Raycast(ray,out depth))
        {
            world_position = ray.origin + ray.direction * depth;

            ret = true;
        }
        else
        {
            world_position = Vector3.zero;

            ret = false;
        }

        return (ret);
    }

    //判断是否可以吸附到正确的位置（离正确的位置够近
    private bool Is_In_Snap_Range()
    {
        bool ret = false;

        //当距离小于我们设定的值时，自动吸附
        if (Vector3.Distance(this.transform.position, this.finished_position) < PieceControl.SNAP_DISTANCE)
        {
            ret = true;
        }

        return ret;
    }

}
