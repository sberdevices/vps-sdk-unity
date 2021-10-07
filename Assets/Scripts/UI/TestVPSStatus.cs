using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;
using UnityEngine.UI;

public class TestVPSStatus : MonoBehaviour
{
	public VPSLocalisationService VPS;
	public Image imageStatus;

	IEnumerator Start()
	{
		// Waiting for Mobile Vps to load 
		yield return new WaitUntil(() => VPS.IsReady());

		// Subscribe to success and error vps result
		VPS.OnPositionUpdated += OnPositionUpdatedHandler;
		VPS.OnErrorHappend += OnErrorHappendHandler;
	}

	private void OnPositionUpdatedHandler(LocationState locationState)
	{
		imageStatus.color = Color.green;
		StartCoroutine(resetImageStatus());
	}

	private void OnErrorHappendHandler(ErrorCode errorCode)
	{
		imageStatus.color = Color.red;
		StartCoroutine(resetImageStatus());
	}

	IEnumerator resetImageStatus()
	{
		yield return new WaitForSeconds(1);
		imageStatus.color = Color.white;
	}
}
