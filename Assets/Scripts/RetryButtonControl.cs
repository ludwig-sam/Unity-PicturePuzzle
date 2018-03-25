using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//控制重来按钮
public class RetryButtonControl : MonoBehaviour {

    public SimpleSprite sprite = null;

    public Texture texture;

    private GameControl game_control;

	// Use this for initialization
	void Start () {
        this.sprite = this.gameObject.AddComponent<SimpleSprite>();

        this.sprite.setTexture(this.texture);
        this.sprite.setSize(this.texture.width * 0.03f / 3.0f, this.texture.height * 0.03f / 3.0f);
        this.sprite.create();


        this.GetComponent<MeshCollider>().sharedMesh = this.GetComponent<MeshFilter>().mesh;

        this.game_control = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameControl>();
	}
	

    private void OnMouseDown()
    {
        this.game_control.OnRetryButtonPush();
    }
}
