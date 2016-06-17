using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Couchbase.Lite;
using Couchbase.Lite.Unity;
using Couchbase.Lite.Util;
using Couchbase.Lite.Listener;
using Couchbase.Lite.Replicator;
using Couchbase.Lite.Store;
using Couchbase.Lite.Auth;
using Couchbase.Lite.Views;
using UnityEngine.UI;

public class DatabaseManager : MonoBehaviour {

	public static DatabaseManager dbManager;

	public Manager _manager = null;
	public Manager getManagerInstance()
	{

		if (this._manager == null) {

			DirectoryInfo directoryInfo = new DirectoryInfo(Application.persistentDataPath + "/Resources/Database" );

			_manager = new Manager(directoryInfo, Manager.DefaultOptions);


		}

		return _manager;
	}

	public Database _db = null;
	public Database getDatabaseInstance(){

		if ((this._db == null) && (this._manager != null)) {

			this._db = _manager.GetDatabase (db_name);

		}

		return _db;
	}
	//database manager document
	public Document managerDocument;
	//a list of all doc ids in the current database instance
	public List<string> allDocIds = new List<string> ();
	//Set the list of document ids in the database
	public void SetAllDocIds()
	{
		Query  allQueryDocs = getDatabaseInstance().CreateAllDocumentsQuery();

		allQueryDocs.AllDocsMode = AllDocsMode.AllDocs;

		var rows = allQueryDocs.Run ();

		foreach (var row in rows) {
			
			allDocIds.Add(row.DocumentId);

		}
	}

	//a list of all the documents
	public List<Document> allDocs = new List<Document> ();
	//set all documents into a list call allDocs
	public void SetAllDocs()
	{
		Query  allQueryDocs = getDatabaseInstance().CreateAllDocumentsQuery();

		allQueryDocs.AllDocsMode = AllDocsMode.AllDocs;

		var rows = allQueryDocs.Run ();

		foreach (var row in rows) {

			allDocs.Add(row.Document);

			print (row.DocumentId);

		}

	}

	//FOR DEBUG ONLY!!!!
	//Delete all Documents in the database
	protected void DisposeAllDocs()
	{
		Query  allQueryDocs = getDatabaseInstance().CreateAllDocumentsQuery();

		allQueryDocs.AllDocsMode = AllDocsMode.AllDocs;

		allQueryDocs.Dispose ();
	}

	//Check to see if current id exist in the database
	public bool IsExistingDoc(string id)
	{
		bool existing = false;

		Query  allQueryDocs = getDatabaseInstance().CreateAllDocumentsQuery();

		allQueryDocs.AllDocsMode = AllDocsMode.AllDocs;

		var rows = allQueryDocs.Run ();

		foreach (var row in rows) {
			if (row.DocumentId == id) {

				print ("this document exists");
				existing = true;
			} else {

				print ("does not exist");
				existing = false;
			}

		}

		return existing;
	}
		
	public Text debugLogText;
	//create a local database with a directory
	public void CreateDatabase()
	{
		//directory pathh
		DirectoryInfo directoryInfo = new DirectoryInfo(Application.persistentDataPath + "/Resources/Database" );

		try{
			_manager = new Manager(directoryInfo, Manager.DefaultOptions);

			_db = _manager.GetDatabase(db_name);

			Debug.Log("Hello Couchbase!");
			debugLogText.text = "Hello Couchbase";

		}catch(CouchbaseLiteException e){

			Log.E (tag, "error getting database", e);

			debugLogText.text = "error getting database";

			return;

		}


	}

	//create a document with a unique id and custom properties
	public Document CreateDocument(string id, IDictionary<string, object> profileProp)
	{
		if (IsExistingDoc (id)) {
			Document newDocument = getDatabaseInstance ().GetDocument (id);


			try {
				newDocument.PutProperties (profileProp);

			} catch (CouchbaseLiteException e) {


				Log.E (tag, "error putting", e);

			}
			return newDocument;

		} else {


			print ("Document alrady exists");
			return null;
		}

	}

	//Retrieve an existing doc
	private Document RetrieveDocument (string id)
	{


		if (IsExistingDoc (id)) {

			Document retrievedDocument;

			retrievedDocument = getDatabaseInstance ().GetDocument (id);

			Log.D (tag, "retrievedDocument=" + (retrievedDocument.Properties.ToString ()));

			return retrievedDocument;
		} else {

			print ("doc does not exist");
			return null;
		}


	}


	//Creates a single property from scatch
	public IDictionary<string, object> CreateProperty(string newKey, object newValue)
	{
		Dictionary<string, object> newProperty = new Dictionary<string, object>(managerDocument.Properties);

		newProperty.Add (newKey, newValue);

		return newProperty;
	}

	//updates a chosen document with a new property.Properties
    //Recommended to use the ^CreateProperty Method^ above
	private void UpdateDocument(string documentId, IDictionary<string, object> newProperty )
	{
		if (IsExistingDoc (documentId)) {
			Document document = getDatabaseInstance ().GetDocument (documentId);


			try {
				//save to the Couchbase local Couchbase Lite DB
				document.PutProperties (newProperty);

			} catch (CouchbaseLiteException e) {


				Log.E (tag, "Error putting", e);

			}

		} else {

			print ("Nothing happened, you must create this document first!");

		}
	}


	//Attach an Image to a document
	public void WriteImageToDocument(Document doc)
	{



	}

	//Delete an Image to document
	public void DeleteImageFromDocument(Document doc)
	{



	}

	//Delete a document from the database
	public void DeleteDocument(string id)
	{
		if (IsExistingDoc (id)) {
			try {

				RetrieveDocument (id).Delete ();

				Log.D (tag, "Deleted document, deletion status = " + RetrieveDocument (id).Deleted);

			} catch (CouchbaseLiteException e) {


				Log.E (tag, "Cannot delete document", e);


			}
		} else {

			print ("Nothing happened, this document must exist first to delete it!");


		}

	}



	/// <summary>
	////Sync Gateways, Push and Pull Replication, Authentication
	/// </summary>

	string db_name = "couchbaseevents";
	string tag = "couchbaseevents";
	//synce url
	private Uri createSyncURL()
	{
		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";

		Uri syncGatewayURL = new Uri (syncGatewayDNS + ":" + syncgatewayPort + "/" + databaseName);


		return syncGatewayURL;
	}



	//Retrieve datase from the sync without Authentication
	private Database pulledDatabase;
	private Database retrieveSycedDatabase()
	{


		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";

		try{

			Replication pull = getDatabaseInstance().CreatePullReplication (createSyncURL());

			//Uses HTTP authentication
			//IAuthenticator authenticator = AuthenticatorFactory.CreateBasicAuthenticator("username", "password");

			//pull.Authenticator = authenticator;


			pull.Start ();


			//print(pull.DocIds);
			pulledDatabase = pull.LocalDatabase;




		}catch(CouchbaseLiteException e){

			Log.E(tag, "Error pulling", e);
		}
			
		return pulledDatabase;

	}



	//Retrieve database from the sync w/ Authentication
	private void createAuthenticationPull(Database database)
	{
		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";
		try{

			Replication pull = database.CreatePullReplication (createSyncURL());

		//Uses HTTP authentication
		IAuthenticator authenticator = AuthenticatorFactory.CreateBasicAuthenticator("username", "password");

		pull.Authenticator = authenticator;


		pull.Start ();



		}catch(CouchbaseLiteException e){
			
			Log.E(tag, "Error pulling", e);
		}

	}

	//Save database into the sync w/ Authentication
	private void createAuthenticationPush(Database database)
	{
		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";
		try{

			Replication push = database.CreatePushReplication (createSyncURL());

			//Uses HTTP authentication
			IAuthenticator authenticator = AuthenticatorFactory.CreateBasicAuthenticator("username", "password");

			push.Authenticator = authenticator;


			push.Start ();



		}catch(CouchbaseLiteException e){


			Log.E(tag, "Error pulling", e);


		}

	}


	//Add a attchment to a specific database and specific document
	private void addAttachment(Database database, string documentId)
	{
		Document document = database.GetDocument (documentId);

		try{

			BitArray inputStream = new BitArray(new byte[]{0,0,0,0});

			UnsavedRevision revision = document.CurrentRevision.CreateRevision();

		
			revision.SetAttachment("binaryData", "application/octet-stream",inputStream.SyncRoot as Stream); 
			revision.Save();



			Attachment attach = revision.GetAttachment("binaryData");

			BufferedStream reader = new BufferedStream(attach.ContentStream);


			StringBuilder values = new StringBuilder ();

			for (int i = 0; i < 4; i++)
			{

			values.Append(reader.ReadByte().ToString() + " ");

			}

			Log.V("LaurentActivity", "The docID: " + documentId + ", attachment contents was: " + values.ToString());


		}catch(CouchbaseLiteException e){


			Log.E(tag, "Error putting", e);


		}
	}


		



	//create a pull to the sync without athentication
	private void createPull(Database database)
	{
		
		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";

		try{
			Uri syncGatewayURL = new Uri (syncGatewayDNS + ":" + syncgatewayPort + "/" + databaseName);

		Replication pull = database.CreatePullReplication (syncGatewayURL);

		pull.Start ();
		}
		catch(UriFormatException e) {


			Log.E ("couchbaseevents", "Error creating Pull", e);


		}
	}

	//create a push to the sync without athentication
	private void createPush(Database database)
	{

		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";

		try{
			

		Uri syncGatewayURL = new Uri (syncGatewayDNS + ":" + syncgatewayPort + "/" + databaseName);

			//print(syncGatewayURL.AbsolutePath);
			Replication push = database.CreatePushReplication (syncGatewayURL);



			push.Start ();


		

		}
		catch(UriFormatException e) {

			Log.E ("couchbaseevents", "Error creating Push", e);

		}
	}

	//create a pull replication to the sync without athentication
	private void createPullRepilcation (Database database)
	{

		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";


		try{
			Uri syncGatewayURL = new Uri (syncGatewayDNS + ":" + syncgatewayPort + databaseName);

			Replication pull = database.CreatePullReplication (syncGatewayURL);



			ReplicationChangeEventArgs changedEvent = new ReplicationChangeEventArgs(pull);
	
		
			int completedChangesCount = changedEvent.Source.CompletedChangesCount;

			int changeCount = changedEvent.Source.ChangesCount;

			pull.Start();
		}
		catch(UriFormatException e) {

			Log.E ("couchbaseevents", "Error creating Push", e);

		}
			
	}

	//create a push replication to the sync without athentication
	private void createPushRepilcation (Database database)
	{

		string syncGatewayDNS = "https://10.0.2.2";
		string syncgatewayPort = "4984";
		string databaseName = "couchbaseevents";


		try{
			Uri syncGatewayURL = new Uri (syncGatewayDNS + ":" + syncgatewayPort + databaseName);

			Replication push = database.CreatePushReplication (syncGatewayURL);



			ReplicationChangeEventArgs changedEvent = new ReplicationChangeEventArgs(push);


			int completedChangesCount = changedEvent.Source.CompletedChangesCount;

			int changeCount = changedEvent.Source.ChangesCount;

			push.Start();
		}
		catch(UriFormatException e) {

			Log.E ("couchbaseevents", "Error creating Push", e);

		}

	}


	// Use this for initialization
	void Awake () {

		dbManager = this;


		CreateDatabase ();

		//debug im not worried about all doc being deleted
		DisposeAllDocs ();


	}

	void Start()
	{



	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
