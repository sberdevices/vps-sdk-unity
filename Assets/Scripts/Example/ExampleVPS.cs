﻿using System.Collections;
using System.Collections.Generic;
using ARVRLab.VPSService;
using UnityEngine;

public class ExampleVPS : MonoBehaviour
{
	/// <summary>
    /// VPS API class to control VPS Service
    /// </summary>
    public VPSLocalisationService VPS;

    private IEnumerator Start()
    {
		// Waiting for Mobile Vps to load 
		yield return new WaitUntil(() => VPS.IsReady());

		// Subscribe to success and error vps result
		VPS.OnPositionUpdated += OnPositionUpdatedHandler;
		VPS.OnErrorHappend += OnErrorHappendHandler;

		// Create custom settings to VPS: generate url in constructor and set delay between requests
		SettingsVPS settings = new SettingsVPS("bootcamp", "eeb38592-4a3c-4d4b-b4c6-38fd68331521", ServerType.Prod);
		settings.Timeout = 3;

		// Start service
		VPS.StartVPS(settings);
	}

	/// <summary>
    /// Request finished successfully
    /// </summary>
    /// <param name="locationState"></param> 
	private void OnPositionUpdatedHandler(LocationState locationState)
	{
		Debug.LogFormat("[Event] Localisation successful! Receive position {0} and Y angle {1}", locationState.Localisation.LocalPosition, locationState.Localisation.LocalRotationY);
	}

	/// <summary>
	/// Request finished with error
	/// </summary>
	/// <param name="errorCode"></param>
	private void OnErrorHappendHandler(ErrorCode errorCode)
    {
		Debug.LogFormat("[Event] Localisation error: {0}", errorCode);
	}

	/// <summary>
    /// In the end stop vps service
    /// </summary>
	private void OnDestroy()
	{
		VPS.StopVps();
	}
}