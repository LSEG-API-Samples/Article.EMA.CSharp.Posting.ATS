# Implementing Real-Time API applications to work with ATS - Part 2 (with C# content only)

## Note

This article content aims for updating the [Implementing Real-Time API applications to work with ATS - Part 2](https://developers.lseg.com/en/article-catalog/article/implementing-elektron-api-applications-work-ats-part-2) article with the following updates:

- Add EMA C# content
- Rebrading Refinitiv to LSEG
- Add ATS required fields

So other APIs content will not be included in this article. Please see the full version on the [Developer Portal](https://developers.lseg.com/) website.

## About this article

**Update**: October 2024

This is the next part of [Implementing Real-Time API application to work with ATS - Part 1](https://developers.lseg.com/en/article-catalog/article/implementing-elektron-api-applications-work-ats-part-1) article. In this part I explain how to update data, delete fields and RIC with Real-Time APIs application source code. I also present troubleshooting which helps you to solve common problems which can occur when working with ATS. I strongly recommend you read the [part 1 article](https://developers.lseg.com/en/article-catalog/article/implementing-elektron-api-applications-work-ats-part-1) first. It gives you the basic knowledge which you need to know before working with ATS e.g. Posting and ATS concepts, Prerequisites, the overview steps . It also explains how to add RIC and fields before your application can update data, delete fields and RIC which are explained in this part 2.

## Sample Posting with Updating data, Deleting Fields and RIC

### Updating field values

You can update field values on ATS by sending a post message from a consumer application as shown in step 3 in the figure above. Here's the sample of a post message that will update the value of field id 22 and 25 of a RIC named *NEW.RIC* with value *430* and *460* respectively:

```xml
<POST domainType="MARKET_PRICE" streamId="1" containerType="MSG" flags="0x66 (HAS_POST_ID|HAS_MSG_KEY|POST_COMPLETE|ACK)" postId="3" postUserId="18" postUserAddr="10.42.61.200" dataSize="24">
	<key flags="0x03 (HAS_SERVICE_ID|HAS_NAME)" serviceId="267" name="NEW.RIC"/>
	<dataBody>
		<UPDATE domainType="MARKET_PRICE" streamId="0" containerType="FIELD_LIST" flags="0x00" updateType="0" dataSize="13">
			<dataBody>
				<fieldList flags="0x08 (HAS_STANDARD_DATA)">
					<fieldEntry fieldId="22" data="0F2B"/>
					<fieldEntry fieldId="25" data="0F2E"/>
				</fieldList>
			</dataBody>
		</UPDATE>
	</dataBody>
</POST>
```

Notice that:

- The post message's domain type is **Market Price**. The streamId is *1* means the post message is sent via the login stream, **off-stream posting**. The message contains the **postId** and the flag **ACK**(to need an ack message) is set. It also contains **Visible Publisher Identifier(VPI)**. VPI consists of **postUserId** and **postUserAddr**.
- The key **name** of the post message must be the RIC name whose data is going to be updated.The key name of the post message must be the RIC name whose data is going to be updated.
- The payload of the post message is an Update of **Market Price** message. The payload of the Update message is a field list.  
- The field list are the updated fields i.e. field id 12(HIGH_1) and field id 13(LOW_1) with their updated values on this RIC.
- Data values in the fields list are encoded OMM.

An example of success Ack message:

```xml
<ACK domainType="MARKET_PRICE" streamId="1" containerType="NO_DATA" flags="0x12 (HAS_TEXT|HAS_MSG_KEY)" ackId="3" text="[1]: Contribution Accepted" dataSize="0">
     <key flags="0x03 (HAS_SERVICE_ID|HAS_NAME)" serviceId="267" name="NEW.RIC"/>
     <dataBody>
     </dataBody>
</ACK>
```

Notice that:

- The Ack message's domain type is **Market Price**. The **ackId** is *3* which corresponds with the **postId**(*3*) of the post message. Hence, this is the result of the post message above.
- There is no **NAK(Negative Acknowledge)** code so **ATS** can perform the operation according to the post message successfully. That's mean **ATS** can update the fields' data of the RIC successfully.

The example of each Real-Time APIs snipped source code to create the post message above for updating data are below:

- EMA C#:

```C#
// Consumer.cs in 341_MP_OffStreamPost folder
public void OnRefreshMsg(RefreshMsg refreshMsg, IOmmConsumerEvent consumerEvent)
{
    ...

    if (refreshMsg.DomainType() == EmaRdm.MMT_LOGIN &&
            refreshMsg.State().StreamState == OmmState.StreamStates.OPEN &&
            refreshMsg.State().DataState == OmmState.DataStates.OK)
    {
        PostMsg postMsg = new();
        UpdateMsg nestedUpdateMsg = new();
        FieldList nestedFieldList = new();

        //FieldList is a collection
        nestedFieldList.AddReal(22, 43, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
        nestedFieldList.AddReal(25, 46, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
        nestedFieldList.Complete();
        nestedUpdateMsg.Payload(nestedFieldList);

		// The Post User address 170540488 (long) is converted from IP address 10.42.61.200
        ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(3).ServiceId(267)
                                    .Name("NEW.RIC").SolicitAck(true).Complete(true).PublisherId(18,170540488)
                                    .Payload(nestedUpdateMsg), consumerEvent.Handle);
    }
    ...
}
```

### Deleting Fields

You can use ATS command, **ATS_DELETE**, to delete fields by sending a post message from a consumer application as shown in step 3 in the figure above. Here’s the sample of a post message that will delete field id *13* and *25* from a RIC named NEW.RIC:

```xml
<POST domainType="MARKET_PRICE" streamId="1" containerType="MSG" flags="0x66 (HAS_POST_ID|HAS_MSG_KEY|POST_COMPLETE|ACK)" postId="4" postUserId="18" postUserAddr="10.42.61.200" dataSize="34">
	<key flags="0x03 (HAS_SERVICE_ID|HAS_NAME)" serviceId="267" name="ATS_DELETE"/>
	<dataBody>
		<UPDATE domainType="MARKET_PRICE" streamId="0" containerType="FIELD_LIST" flags="0x00" updateType="0" dataSize="23">
			<dataBody>
				<fieldList flags="0x08 (HAS_STANDARD_DATA)">
					<fieldEntry fieldId="-1" data="4E45 572E 5249 43"/>
					<fieldEntry fieldId="13" data="0F01"/>
					<fieldEntry fieldId="25" data="0F02"/>
				</fieldList>
			</dataBody>
		</UPDATE>
	</dataBody>
</POST>
```

Notice that:

- The post message's domain type is **Market Price**. The streamId is *1* means the post message is sent via the **login stream**, off-stream posting. The message contains the **postId** and the flag **ACK**(to need an ack message) is set. It also contains **Visible Publisher Identifier(VPI)**. VPI consists of **postUserId** and **postUserAddr**.
- The key **name** of the post message must be **ATS_DELETE** to inform **ATS** to add the fields.
- The payload of the post message is an Update of **Market Price** message. The payload of the Update message is a field list.  
- The field list consists of:
	- Field Id **-1** for the RIC/record name whose the fields are deleted.
	- The fields i.e. field id *13(LOW_1)* and field id *25(ASK)* to be deleted from this RIC.
	- Data values shown in the fields list are encoded OMM.
	
An example of success Ack message:

```xml
<ACK domainType="MARKET_PRICE" streamId="1" containerType="NO_DATA" flags="0x12 (HAS_TEXT|HAS_MSG_KEY)" ackId="4" text="[3]: Delete Accepted" dataSize="0">
     <key flags="0x03 (HAS_SERVICE_ID|HAS_NAME)" serviceId="267" name="ATS_DELETE"/>
     <dataBody>
     </dataBody>
</ACK>
```

Notice that:

- The Ack message's domain type is **Market Price**. The **ackId** is *4* which corresponds with the **postId**(*4*) of the post message. Hence, this is the result of the post message above.
- There is no **NAK(Negative Acknowledge)** code so **ATS** can perform the operation according to the post message successfully. That's mean **ATS** can delete the fields from the RIC successfully.

The example of each Real-Time API snipped source code to create the post message above for deleting the fields are below:

- EMA C#:

```C#
// Consumer.cs in 341_MP_OffStreamPost folder
public void OnRefreshMsg(RefreshMsg refreshMsg, IOmmConsumerEvent consumerEvent)
{
    ...

    if (refreshMsg.DomainType() == EmaRdm.MMT_LOGIN &&
            refreshMsg.State().StreamState == OmmState.StreamStates.OPEN &&
            refreshMsg.State().DataState == OmmState.DataStates.OK)
    {
        PostMsg postMsg = new();
        UpdateMsg nestedUpdateMsg = new();
        FieldList nestedFieldList = new();

       //FieldList is a collection
        nestedFieldList.AddAscii(-1, "NEW.RIC");
        nestedFieldList.AddReal(13, 1, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
        nestedFieldList.AddReal(25, 2, OmmReal.MagnitudeTypes.EXPONENT_POS_1);
        nestedFieldList.Complete();
        nestedUpdateMsg.Payload(nestedFieldList);

		// The Post User address 170540488 (long) is converted from IP address 10.42.61.200
        ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(4).ServiceId(267)
                                    .Name("ATS_DELETE").SolicitAck(true).Complete(true).PublisherId(18,170540488)
                                    .Payload(nestedUpdateMsg), consumerEvent.Handle);
    }
    ...
}
```

### Deleting a RIC

You can use ATS command, **ATS_DELETE_ALL**, to delete a RIC/record by sending a post message from a consumer application as shown in step *3* in the figure above. Here's the sample of a post message that will delete a RIC named NEW.RIC:

```xml
<POST domainType="MARKET_PRICE" streamId="1" containerType="MSG" flags="0x66 (HAS_POST_ID|HAS_MSG_KEY|POST_COMPLETE|ACK)" postId="5" postUserId="18" postUserAddr="10.42.61.200" dataSize="24">
	<key flags="0x03 (HAS_SERVICE_ID|HAS_NAME)" serviceId="267" name="ATS_DELETE_ALL"/>
	<dataBody>
		<UPDATE domainType="MARKET_PRICE" streamId="0" containerType="FIELD_LIST" flags="0x00" updateType="0" dataSize="13">
			<dataBody>
				<fieldList flags="0x08 (HAS_STANDARD_DATA)">
					<fieldEntry fieldId="-1" data="4E45 572E 5249 43"/>
				</fieldList>
			</dataBody>
		</UPDATE>
	</dataBody>
</POST>
```
Notice that:

- The post message's domain type is **Market Price**. The streamId is *1* means the post message is sent via the login stream, **off-stream** posting. The message contains the **postId** and the flag **ACK**(to need an ack message) is set. It also contains **Visible Publisher Identifier(VPI)**. VPI consists of **postUserId** and **postUserAddr**.
- The key name of the post message must be **ATS_DELETE_ALL** to inform **ATS** to delete a RIC/Record.
- The payload of the post message is an Update of **Market Price** message. The payload of the Update message is a field list.
- The field list consists of the field Id **-1** for the RIC/record name to delete from **ATS**.
- Data values shown in the fields list are encoded OMM.

An example of success Ack message:

```xml
<ACK domainType="MARKET_PRICE" streamId="1" containerType="NO_DATA" flags="0x12 (HAS_TEXT|HAS_MSG_KEY)" ackId="5" text="[3]: Delete Accepted" dataSize="0">
     <key flags="0x03 (HAS_SERVICE_ID|HAS_NAME)" serviceId="267" name="ATS_DELETE_ALL"/>
     <dataBody>
     </dataBody>
</ACK>
```

Notice that:

- The Ack message's domain type is **Market Price**. The **ackId** is *5* which corresponds with the **postId**(*5*) of the post message. Hence, this is the result of the post message above.
- There is no **NAK(Negative Acknowledge)** code so **ATS** can perform the operation according to the post message successfully. That's mean **ATS** can delete the RIC/record from **ATS** successfully.

The example of each Real-Time API snipped source code to create the post message above for deleting a RIC are below:

- EMA C#:

```C#
// Consumer.cs in 341_MP_OffStreamPost folder
public void OnRefreshMsg(RefreshMsg refreshMsg, IOmmConsumerEvent consumerEvent)
{
    ...

    if (refreshMsg.DomainType() == EmaRdm.MMT_LOGIN &&
            refreshMsg.State().StreamState == OmmState.StreamStates.OPEN &&
            refreshMsg.State().DataState == OmmState.DataStates.OK)
    {
        PostMsg postMsg = new();
        UpdateMsg nestedUpdateMsg = new();
        FieldList nestedFieldList = new();

       ///FieldList is a collection
        nestedFieldList.AddAscii(-1, "NEW.RIC");
        nestedFieldList.Complete();
        nestedUpdateMsg.Payload(nestedFieldList);

		// The Post User address 170540488 (long) is converted from IP address 10.42.61.200
        ((OmmConsumer)consumerEvent!.Closure!).Submit(postMsg.PostId(postId++).ServiceId(267)
                                    .Name("ATS_DELETE_ALL").SolicitAck(true).Complete(true).PublisherId(18,170540488)
                                    .Payload(nestedUpdateMsg), consumerEvent.Handle);
    }
    ...
}
```

## Troubleshooting

[Action] Add new common error as follows.

**6) NackCode: 132, Text: [900]: Service Denied when sending the Add RIC, Add Fields, Delete Fields, and Delete RIC**

An example of Ack message:

```xml
<ACK domainType="MARKET_PRICE" streamId="1" containerType="NO_DATA" flags="0x32 (HAS_TEXT|HAS_MSG_KEY|HAS_NAK_CODE)" ackId="1" nakCode="132" text="[900]: Service Denied" dataSize="0">
        <key flags="0x07 (HAS_SERVICE_ID|HAS_NAME|HAS_NAME_TYPE)" serviceId="257" name="ATS_INSERT_S" nameType="1"/>
        <dataBody>
        </dataBody>
</ACK>
```

- **Root Cause**: The RTDS (ADS and ADH)'s RDMFieldDictionary file does not have a complete ATS field definitions content set.
- **Solution** Add the following ATS field definitions to the RTDS RDMFieldDictionary file, and then retart the RTDS components. 

```ini
!ACRONYM    DDE ACRONYM          FID  RIPPLES TO  FIELD TYPE     LENGTH  RWF TYPE   RWF LEN
!-------    -----------          ---  ----------  ----------     ------  --------   -------
!
X_RIC_NAME "RIC NAME"              -1  NULL        ALPHANUMERIC       32  RMTES_STRING    32
X_ERRORMSG "X_ERRORMSG"            -2  NULL        ALPHANUMERIC       80  RMTES_STRING    80
X_LOLIM_FD "X_LOLIM_FD"            -3  NULL        ALPHANUMERIC       3   RMTES_STRING    3
X_HILIM_FD "X_HILIM_FD"            -4  NULL        ALPHANUMERIC       3   RMTES_STRING    3
X_LOW_LIM  "X_LOW_LIM"             -5  NULL        ALPHANUMERIC       17  RMTES_STRING    17
X_HIGH_LIM "X_HIGH_LIM"            -6  NULL        ALPHANUMERIC       17  RMTES_STRING    17
X_ARRAY    "X_ARRAY"               -7  NULL        ALPHANUMERIC       25  RMTES_STRING    25
X_BU       "X_BU"                  -8  NULL        ALPHANUMERIC       20  RMTES_STRING    20
X_CONTAINER "X_CONTAINER"          -9  NULL        ALPHANUMERIC       20  RMTES_STRING    20
X_PE       "X_PE"                  -10 NULL        ALPHANUMERIC       20  RMTES_STRING    20
X_MODEL    "X_MODEL"               -11 NULL        ALPHANUMERIC       20  RMTES_STRING    20
X_LINK     "X_LINK"                -12 NULL        ALPHANUMERIC       20  RMTES_STRING    20
X_ARGS     "X_ARGS"                -13 NULL        ALPHANUMERIC       20  RMTES_STRING    20
X_HOLIDAYS "X_HOLIDAYS"            -14 NULL        ALPHANUMERIC       255 RMTES_STRING    255
X_PPE      "X_PPE"                 -15 NULL        ALPHANUMERIC       20  RMTES_STRING    20
```

## Summary

After finish both articles in this series, you will understand more about ATS and Posting. Also, by using the LSEG Real-Time APIs you can fulfill general day-to-day operations with ATS more easily and quickly, including solving common problems. The technical knowledge explained in the article can help you to perform additional advanced use case scenarios to better utilize ATS’s usage further as well. If you would like to acquire ATS, please contact LSEG Account team for the process and details. You can contact the support team directly by submitting your query to Get Support in [myaccount.lseg.com](https://myaccount.lseg.com/) website if you require any ATS assistance.

## References

For further details, please check out the following resources:

- [EMA Java Reference Manual](https://developers.lseg.com/en/api-catalog/refinitiv-real-time-opnsrc/rt-sdk-java/documentation#message-api-java-development-and-configuration-guides-with-examples)
- [ETA Java Reference Manual](https://developers.lseg.com/en/api-catalog/refinitiv-real-time-opnsrc/rt-sdk-java/documentation#enterprise-transport-api-java-edition-developer-guides)
- [EMA C++ Reference Manual](https://developers.lseg.com/en/api-catalog/refinitiv-real-time-opnsrc/rt-sdk-cc/documentation#enterprise-message-api-c-development-guides)
- [ETA C Reference Manual](https://developers.lseg.com/en/api-catalog/refinitiv-real-time-opnsrc/rt-sdk-cc/documentation#enterprise-transport-api-c-edition-developer-guides)
- [EMA C# Reference Manual](https://developers.lseg.com/en/api-catalog/refinitiv-real-time-opnsrc/refinitiv-real-time-csharp-sdk/documentation#message-api-c-development-guides)
- [WebSocket API Reference Manual](https://developers.lseg.com/en/api-catalog/refinitiv-real-time-opnsrc/refinitiv-websocket-api/documentation#web-socket-api-developer-guide)
- [ATS documents](https://myaccount.lseg.com/en/product/real-time-advanced-transformation-server)