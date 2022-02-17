using System;
using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;
using UnityEngine.UI;

public class TestVPSStatus : MonoBehaviour
{
	private VPSLocalisationService VPS;
	public Image imageStatus;
	private int Count = 0;
	private bool isLocalized = false;

	System.Diagnostics.Stopwatch fullStopWatch = new System.Diagnostics.Stopwatch();

	private IEnumerator Start()
	{
		VPS = FindObjectOfType<VPSLocalisationService>();
		if (!VPS)
			yield break;

		// Waiting for Mobile Vps to load 
		yield return new WaitUntil(() => VPS.IsReady());

		// Subscribe to success and error vps result
		VPS.OnReset += OnReset;
		VPS.OnPositionUpdated += OnPositionUpdatedHandler;
		VPS.OnErrorHappend += OnErrorHappendHandler;
	}

	private void OnReset()
	{
		Count = 0;
		isLocalized = false;
		fullStopWatch.Restart();
	}

	private void OnPositionUpdatedHandler(LocationState locationState)
	{
		if (!isLocalized)
		{
			fullStopWatch.Stop();
			TimeSpan fullTS = fullStopWatch.Elapsed;

			string fullTimeStr = String.Format("{0:N10}", fullTS.TotalSeconds);
			VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric" + SettingsToggles.GetLocType() + "] FullSerialMVPSRequestTime {0}", fullTimeStr);
			VPSLogger.LogFormat(LogLevel.VERBOSE, "[Metric" + SettingsToggles.GetLocType() + "] SerialAttemptCount {0}", Count);
			isLocalized = true;
		}

		imageStatus.color = Color.green;
		StartCoroutine(resetImageStatus());
	}

	private void OnErrorHappendHandler(ErrorCode errorCode)
	{
		if (!isLocalized)
		{
			Count++;
		}

		imageStatus.color = Color.red;
		StartCoroutine(resetImageStatus());
	}

	public IEnumerator resetImageStatus()
	{
		yield return new WaitForSeconds(1);
		imageStatus.color = Color.white;
	}
}
