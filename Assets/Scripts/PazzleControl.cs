using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//控制拼图
public class PazzleControl : MonoBehaviour
{
    //获取GameControl物体
    private GameControl game_control = null; 

    private int piece_num;          //碎片的数量
    private int piece_finished_num; //完成的碎片数量

    enum STEP
    {
        NONE = -1,

        PLAY = 0,   //游戏中
        CLEAR,      //清空游戏

        NUM,
    };

    //当前状态
    private STEP step = STEP.NONE;
    //下个状态
    private STEP next_step = STEP.NONE;

    private float step_timer = 0.0f;
    private float step_timer_prev = 0.0f;

    //-------------------------------------------------//

    //所有的碎片
    private PieceControl[] all_pieces;

    //考虑中的碎片，按距离远近排列
    private PieceControl[] active_pieces;

    //碎片洗牌后的位置
    private Bounds shuffle_zone;

    //旋转网格的角度
    private float pazzle_rotation = 37.0f;

    //设置洗牌的参数，网格的数量
    private int shuffle_grid_num = 1;

    //---------------------------------------------------//

    // Use this for initialization
    void Start()
    {
        //通过标签找到GameController物体， 获取GameController脚本
        this.game_control = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameControl>();

        //统计碎片的对象数量
        this.piece_num = 0;
        //
        for (int i = 0; i < this.transform.childCount; i++)
        {
            GameObject piece = this.transform.GetChild(i).gameObject;

            if (!this.Is_piece_object(piece))
            {
                continue;
            }
            this.piece_num++;
        }

        this.all_pieces = new PieceControl[this.piece_num];
        this.active_pieces = new PieceControl[this.piece_num];

        //向各个碎片添加PieceControl组件（脚本“PieceControl.cs”)

        for (int i = 0, n = 0; i < this.transform.childCount; i++)
        {
            GameObject piece = this.transform.GetChild(i).gameObject;

            if (!this.Is_piece_object(piece))
            {
                continue;
            }

            piece.AddComponent<PieceControl>();
            piece.GetComponent<PieceControl>().pazzle_control = this;


            this.all_pieces[n++] = piece.GetComponent<PieceControl>();
        }

        this.piece_finished_num = 0;

        //设置碎片的绘制顺序
        this.Set_Height_Offset_To_Pieces();

        for (int i = 0; i < this.transform.childCount; i++)
        {
            GameObject game_object = this.transform.GetChild(i).gameObject;

            if (this.Is_piece_object(game_object))
            {
                continue;
            }
            game_object.GetComponent<Renderer>().material.renderQueue = this.GetDrawPriorityBase();
        }
        //算出碎片洗牌的位置（范围）
        this.Calc_Shuffle_Zone();
    }

    // Update is called once per frame
    void Update()
    {
        //-------------------------------------------------------------------------//

        this.step_timer_prev = this.step_timer;

        this.step_timer += Time.deltaTime;

        //-------------------------------------------------------------------------//

        #region//检测状态迁移
        switch (this.step)
        {
            case STEP.NONE:
                {
                    this.next_step = STEP.PLAY;
                }
                break;
            case STEP.PLAY:
                {
                    //如果碎片全部都被放置到正解位置，清空
                    if (this.piece_finished_num >= this.piece_num)
                    {
                        this.next_step = STEP.CLEAR;
                    }
                }
                break;
        }
        #endregion

        //-------------------------------------------------------------------------//
        //迁移时的初始化
        //当next_step不为NONE时，意味着要改变当前游戏状态
        if (this.next_step != STEP.NONE)
        {
            switch (this.next_step)
            {
                case STEP.PLAY:
                    {
                        //Play时，进行游戏初始化
                        for (int i = 0; i < this.all_pieces.Length; i++)
                        {
                            this.active_pieces[i] = this.all_pieces[i];
                        }

                        this.piece_finished_num = 0;

                        this.Shuffle_Piece();

                        foreach (PieceControl piece in this.active_pieces)
                        {
                            piece.Restart();
                        }

                        //设置碎片的绘制顺序
                        this.Set_Height_Offset_To_Pieces();
                    }
                    break;
            }
            //给当前状态赋值
            this.step = this.next_step;
            //再把下一个状态赋值为NONE
            this.next_step = STEP.NONE;

            this.step_timer = 0.0f;
        }
        //----------------------------------------------------------------
        //执行处理

        switch (this.step)
        {
            case STEP.CLEAR:
                {
                    //完成时的音乐

                    const float play_se_delay = 0.4f;

                    if (this.step_timer_prev < play_se_delay && play_se_delay <= this.step_timer)
                    {
                        this.game_control.PlaySe(GameControl.SE.COMPLETE);
                            
                    }
                }
                break;
        }

        PazzleControl.DrawBounds(this.shuffle_zone);

    }

    //“重新开始”按钮被按下时
    public void BeginRetryAction()
        {
        //把下个状态设置为Play
        this.next_step = STEP.PLAY;
        }


    //开始拖动碎片时,把拖动中的碎片放在最上面
    public void PickPiece(PieceControl _piece)
    {
        int i, j;

        //将被点击的碎片移动到数组的头部
        //由于this.pieces[]按照显示的顺序排列，头部的元素将被显示在最上方
        for (i = 0; i < this.active_pieces.Length; i++)
        {
            if (this.active_pieces[i] == null)
            {
                continue;
            }

            if (this.active_pieces[i].name == _piece.name)
            {
                //将位于“被点击碎片”之前的碎片，逐个向后移动

                for (j = i; j > 0; j--)
                {
                    this.active_pieces[j] = this.active_pieces[j - 1];
                }

                //被点击的碎片回到数组头部
                this.active_pieces[0] = _piece;

                break;
            }
        }
        //重新设置高度
        this.Set_Height_Offset_To_Pieces();
    }

    /// <summary>
    /// 完成拼图
    /// </summary>
    /// <param name="piece"></param>
    public void FinishPiece(PieceControl piece)
    {
        int i, j;

        piece.GetComponent<Renderer>().material.renderQueue = this.GetDrawPriorityFinishedPiece();

        //将被点击的碎片从数组中剔除
        for (i = 0; i < this.active_pieces.Length; i++)
        {
            if (this.active_pieces[i] == null)
            {
                continue;
            }

            if (this.active_pieces[i].name == piece.name)
            {
                //将位于被点击的碎片之后的碎片逐个往前移动
                for (j = i; j < this.active_pieces.Length - 1; j++)
                {
                    //直接把j后一位的数值赋值到j的位置，以此类推直到最后一位
                    this.active_pieces[j] = this.active_pieces[j + 1];
                }

                //清空数组的末尾
                this.active_pieces[this.active_pieces.Length - 1] = null;

                //得到正解的碎片数量+1
                this.piece_finished_num++;

                break;
            }
        }
        this.Set_Height_Offset_To_Pieces();
    }
    //-------------------------------------------------------------------------//

    //用来调整网格位置的偏移值
    private static float Shuffle_Zone_Offset_X = -8.0f;
    private static float Shuffle_Zone_Offset_Y = 1.0f;
    private static float Shuffle_Zone_Scale = 1.1f;

    //计算处对碎片洗牌的位置
    private void Calc_Shuffle_Zone()
    {
        Vector3 center;


        center = Vector3.zero;

        foreach (PieceControl piece in this.all_pieces)
        {
            center += piece.finished_position;

        }
        center /= (float)this.all_pieces.Length;

        center.x += Shuffle_Zone_Offset_X;
        center.z += Shuffle_Zone_Offset_Y;

        //设置碎片的网格数量

        this.shuffle_grid_num = Mathf.CeilToInt(Mathf.Sqrt((float)this.all_pieces.Length));

        //碎片的边框矩形中最大值 = 1个网格的尺寸

        Bounds piece_bounds_max = new Bounds(Vector3.zero, Vector3.zero);

        foreach (PieceControl piece in this.all_pieces)
        {
            Bounds bounds = piece.GetBounds(Vector3.zero);

            piece_bounds_max.Encapsulate(bounds);
        }

        piece_bounds_max.size *= Shuffle_Zone_Scale;

        this.shuffle_zone = new Bounds(center, piece_bounds_max.size * this.shuffle_grid_num);
    }

    //对碎片位置进行随机洗牌
    private void Shuffle_Piece()
    {

        #region //将碎片按照网格顺序排列

        int[] piece_index = new int[this.shuffle_grid_num * this.shuffle_grid_num];

        for (int i = 0; i < piece_index.Length; i++)
        {
            if (i < this.all_pieces.Length)
            {
                piece_index[i] = i;
            }
            else
            {
                //没有存放碎片的网格赋值为-1
                piece_index[i] = -1;
            }
        }
        #endregion

        #region //随机选取两个碎片，交换位置
        for (int i = 0; i < piece_index.Length - 1; i++)
        {
            int j = Random.Range(i + 1, piece_index.Length);

            int temp = piece_index[j];

            piece_index[j] = piece_index[i];

            piece_index[i] = temp;
        }

        #endregion

        #region//通过位置的索引变换为实际的坐标来进行配置
        Vector3 pitch;

        pitch = this.shuffle_zone.size / (float)this.shuffle_grid_num;

        for (int i = 0; i < piece_index.Length; i++)
        {
            if (piece_index[i] < 0)
            {
                continue;
            }

            PieceControl piece = this.all_pieces[piece_index[i]];

            Vector3 position = piece.finished_position;

            int ix = i % this.shuffle_grid_num;
            int iz = i / this.shuffle_grid_num;

            position.x = ix * pitch.x;
            position.z = iz * pitch.z;

            position.x += this.shuffle_zone.center.x - pitch.x * (this.shuffle_grid_num / 2.0f - 0.5f);
            position.z += this.shuffle_zone.center.z - pitch.z * (this.shuffle_grid_num / 2.0f - 0.5f);

            position.y = piece.finished_position.y;

            piece.start_position = position;
        }

        #endregion

        #region //逐步（网格的格子内）随机移动位置

        Vector3 offset_cycle = pitch / 2.0f;
        Vector3 offset_add = pitch / 5.0f;

        Vector3 offset = Vector3.zero;

        for (int i = 0; i < piece_index.Length; i++)
        {
            if (piece_index[i] < 0)
            {
                continue;
            }

            PieceControl piece = this.all_pieces[piece_index[i]];

            Vector3 position = piece.start_position;

            position.x += offset.x;
            position.z += offset.z;

            piece.start_position = position;
            //

            offset.x += offset_add.x;

            if (offset.x > offset_cycle.x / 2.0f)
            {
                offset.x -= offset_cycle.x;
            }

            offset.z += offset_add.z;
            if (offset.z > offset_cycle.z / 2.0f)
            {
                offset.z -= offset_cycle.z;
            }
        }



        #endregion

        #region//使全体旋转

        foreach (PieceControl piece in this.all_pieces)
        {
            Vector3 position = piece.start_position;

            position -= this.shuffle_zone.center;

            position = Quaternion.AngleAxis(this.pazzle_rotation, Vector3.up) * position;

            position += this.shuffle_zone.center;

            piece.start_position = position;
        }
        #endregion

        this.pazzle_rotation += 90;
    }

    //判断是否是碎片的GameObject
    private bool Is_piece_object(GameObject game_object)
    {
        bool is_piece = false;
        do
        {
            //名字中含有“base”表示底座对象
            if (game_object.name.Contains("base"))
            {
                continue;
            }
            is_piece = true;
        } while (false);
        return (is_piece);
    }

    //---------------------------------------------------------------//

    /// <summary>
    /// 给所有的碎片设置高度的偏移
    /// </summary>
    private void Set_Height_Offset_To_Pieces()
    {
        float offset = 0.01f;

        int n = 0;

        foreach (PieceControl piece in this.active_pieces)
        {
            if (piece == null)
            {
                continue;
            }

            //指定绘制的顺序
            //piece中越前面的碎片=越靠近上方的碎片被绘制的顺序越晚
            //renderQueue渲染队列
            piece.GetComponent<Renderer>().material.renderQueue = this.GetDrawPriorityPiece(n);

            //为了能够使点击时位于最上方的碎片响应鼠标点击
            //指点Y轴高度的偏移
            //（不这样处理的话可能会由于绘制优先度的关系而导致下面的碎片响应了点击）
            offset -= 0.01f / this.piece_num;

            piece.SetHeightOffset(offset);

            n++;
        }
    }
    //获取绘制优先度（底座）
    private int GetDrawPriorityBase()
    {
        return (0);
    }

    //获取绘制的优先度（被放置到正确的位置）
    private int GetDrawPriorityFinishedPiece()
    {
        int priority;

        priority = this.GetDrawPriorityBase() + 1;

        return (priority);
    }

    //获取绘制的优先度（“重新开始”
    private int GetDrawPriorityRetryButton()
    {
        int priority;

        priority = this.GetDrawPriorityFinishedPiece() + 1;

        return (priority);
    }

    //获取绘制的优先度（被放置到正确的位置）
    private int GetDrawPriorityPiece(int priority_piece)
    {
        int priority;

        priority = this.GetDrawPriorityRetryButton() + 1;

        //renderQueue的值越小则越先绘制
        priority += this.piece_num - 1 - priority_piece;

        return (priority);
    }
    //------------------------------------------------------------//

    //拼图是否完成了？
    public bool isCleared()
    {
        return (this.step == STEP.CLEAR);
    }

    //------------------------------------------------------------//
    public static void DrawBounds(Bounds bounds)
    {
        Vector3[] square = new Vector3[4];

        square[0] = new Vector3(bounds.min.x, 0.0f, bounds.min.z);
        square[1] = new Vector3(bounds.max.x, 0.0f, bounds.min.z);
        square[2] = new Vector3(bounds.max.x, 0.0f, bounds.max.z);
        square[3] = new Vector3(bounds.min.x, 0.0f, bounds.max.z);

        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(square[i], square[(i + 1) % 4], Color.white, 0.0f, false);
        }
    }
}
