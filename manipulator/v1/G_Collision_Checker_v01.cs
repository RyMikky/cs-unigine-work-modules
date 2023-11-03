using System;
using System.Collections;
using System.Collections.Generic;
using Unigine;

[Component(PropertyGuid = "9094caffafc5dc528345cf89fc0c1354bbf366a3")]
public class G_Collision_Checker_v01 : Component
{

    
    
	// For debugging
    public bool debug = true; 
 
    // Time, position and some info of the last occurred contact
    private DateTime lastContactTime; 
    private vec3 lastContactPoint;
    private string lastContactInfo;
        
    private void Init()
    {         
        Body body = node.ObjectBody;
        if (body)
        {   
			// For debug purposes, we can render certain contacts depending on their type    
            body.AddContactsCallback((b) => b.RenderInternalContacts()); 
			// A callback to be fired when each contact emerges
            body.AddContactEnterCallback(OnContactEnter); 
			
			Log.Message("Collision Checker on air\n");
        }   
        else 
		{
			Log.Message("Initiation Error\n");
		}
    }
    
	// This function takes the body and the index of the contact
    private void OnContactEnter(Body body, int num) 
    {
		Log.Message("Contact detected\n");
		// Enable Visualizer to see the rendered contact points
		Visualizer.Enabled = true;
		
        Body test = body.GetContactBody0(num);
        
        if(test.Name == "LeftFinger"){
            Log.Message("LeftFinger!!! We need stop!!!\n");
        }

        //if(body.IsContactInternal(num))
        if(body.IsContactEnter(num))
            
        {
            if (debug)
            {	
				// The time of the contact
                lastContactTime = DateTime.Now; 
                // The position of the contact
				lastContactPoint = body.GetContactPoint(num); 
                
                Body body0 = body.GetContactBody0(num);
                Body body1 = body.GetContactBody1(num);
 
                Body touchedBody = null;
 
                if (body0 && body0 != body) touchedBody = body0;  
                if (body1 && body1 != body) touchedBody = body1;
                
                if (touchedBody)
                {
                    lastContactInfo = $"body {touchedBody.Name} of {touchedBody.Object.Name}";
                    Visualizer.RenderObject(touchedBody.Object, vec4.BLUE, 0.5f); 
                }
                 else
                 {
                     lastContactInfo = $"surface #{body.GetContactSurface(num)} of {body.GetContactObject(num).Name}";
                     Visualizer.RenderObjectSurface(body.GetContactObject(num), body.GetContactSurface(num), vec4.BLUE, 0.5f); 
                 }
                 
                 lastContactInfo += $"\nimpulse: {body.GetContactImpulse(num):0.0}";
            }
            
        }
    }
 
    private void Update()
    {
        // Here we display the info and create a slow motion effect for one second    
        if (debug)
        {
			int count = node.ObjectBody.NumContacts;
			
			//Log.Message("Contact count is - {0}\n", count);
			
            if((DateTime.Now - lastContactTime).Seconds < 1.0f)
            {
				//Game.Scale = 0.25f; 
                //Game.Scale = 1.0f;
                Visualizer.RenderMessage3D(lastContactPoint, vec3.ONE, $"last contact: \n{lastContactInfo}", vec4.GREEN, 2, 24);
            }           				
            else
            {	
                //Game.Scale = 0.5f;
                Game.Scale = 1.0f;
            }           
        }
        
        
    }
}
