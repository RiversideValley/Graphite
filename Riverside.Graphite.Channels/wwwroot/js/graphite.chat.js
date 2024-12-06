(function(){

	document.addEventListener('DOMContentLoaded', function () {

        
            $('#collapseMax').on('shown.bs.collapse', function () {
                setTimeout(() => {
                    $('#messageColumn').removeClass('w-100');
                    $('#btnCollapseRooms').removeClass('fa-caret-up');
                    messageInput.focus();
                }, 300)
            })
            $('#collapseMax').on('hidden.bs.collapse', function () {
                setTimeout(() => {
                    $('#messageColumn').addClass('w-100');
                    $('#btnCollapseRooms').addClass('fa-caret-up');
                    messageInput.focus();
                }, 300)
            })

            function htmlToMarkdown(html) {
                let markdown = html;
                markdown = markdown.replace(/<h1>(.*?)<\/h1>/g, '# $1\n');
                markdown = markdown.replace(/<strong>(.*?)<\/strong>/g, '**$1**');
                markdown = markdown.replace(/<em>(.*?)<\/em>/g, '*$1*');
                markdown = markdown.replace(/<p>(.*?)<\/p>/g, '$1\n');
                markdown = markdown.replace(/<img src="(.*?)" alt="(.*?)"[^>]*>/g, '![$2]($1)');
                // Add more replacements as needed
                return markdown;
            }

            function generateThumbnail(file) {
                return new Promise((resolve, reject) => {
                    if (!file.type.startsWith('image/')) {
                        reject('Not an image file');
                        return;
                    }

                    try {
                        const reader = new FileReader();
                        reader.onload = (event) => {
                            const img = new Image();
                            img.onload = () => {
                                const canvas = document.createElement('canvas');
                                const ctx = canvas.getContext('2d');
                                const maxDim = 100;
                                if (img.width > img.height) {
                                    canvas.width = maxDim;
                                    canvas.height = (img.height / img.width) * maxDim;
                                } else {
                                    canvas.height = maxDim;
                                    canvas.width = (img.width / img.height) * maxDim;
                                }
                                ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
                                resolve(canvas.toDataURL('image/jpeg'));
                            };
                            img.src = event.target.result;
                        };
                        reader.readAsDataURL(file);
                    } catch (err) {
                        reject(err);
                    }
                });
            }
            function blinktext(id2Element) {
                var f = document.getElementById(`${id2Element}`);
                setInterval(function () {
                    f.style.visibility = f.style.visibility == 'hidden' ? '' : 'hidden';
                }, 1000);
            }
            function getByValue(map, searchValue) {
                for (let [key, value] of map.entries()) {
                    if (value === searchValue)
                        return key;
                }
            }
            function getRandomColor() {
                const letters = '0123456789ABCDEF';
                let color = '#';
                for (let i = 0; i < 6; i++) {
                    color += letters[Math.floor(Math.random() * 16)];
                }
                return color;
            }

            function getRandomInt(min, max) {
                min = Math.ceil(min); // Ensure min is an integer
                max = Math.floor(max); // Ensure max is an integer
                return Math.floor(Math.random() * (max - min + 1)) + min;
            };

			let authUser = UserName.toLowerCase(); 
            let sessionMap = new Map([
                ['Public', 'Public']
            ]);
            let currentSession = 'Public';
            let currentUser = 'Public';
            let currentSequenceMessage;
            const generateGuid = function () {
                return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
                    const r = Math.random() * 16 | 0,
                        v = c === 'x' ? r : (r & 0x3 | 0x8);
                    return v.toString(16);
                });
            }

            //  Html encode message.
            const encodedMessage = function (message) {
                return message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
            }

            const addNewSessionCard = function (roomName, sessionId, lastestMessage) {
                const entry = document.createElement('li');
                entry.id = sessionId;
                entry.classList.add("p-2", "m-2", "rounded-3", "shadow-sm","list-group-item" );

                const now = new Date().getTimezoneOffset().toLocaleString();
                if (sessionId === 'Public') {
                    entry.classList.add("bg-secondary")
                } else {
                    entry.style.background = getRandomColor();
                }

                entry.innerHTML =
                    `<div class="justify-content-center  rounded-1 mb-2 p-2">
                        <span id="sessiondelete-${sessionId}" type="button" class="float-start" style="hover:color:#ccc;" ><i class="fas fa-trash"></i></span>
                        <div>

                            <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-${getRandomInt(1, 16)}.webp" alt="avatar"
                                    class="rounded-circle d-flex align-self-center m-3 shadow-1-strong" width="60">
                            <div class="pt-1">
                                <div class="p-2 rounded-3 shadow-sm bg-light w-100" style="font-weight:bolder;text-shadow: 1px 1px 1px white;text-overflow:hidden;font-size:large;font-family:'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;" id="userName-${sessionId}">${roomName}</div>
                                <div class="text-light text-bold flex-sm-wrap" style="text-overflow:ellipsis;" id="message-${sessionId}"><p>${lastestMessage}</p></div>
                            </div>

                        </div>
                    </div>`

                entry.addEventListener('click', (event) => {
                    changeSession(roomName, connection);
                    console.log(`switch to session to: ${roomName}`);
                    const el = document.querySelector(`[data-session-id="${sessionMap.get(roomName)}"]`);
                    const name = document.getElementById("rcNameOptions");
                    name.value = roomName;
                });

                document.getElementById("userlist").appendChild(entry);

                var session = document.getElementById(`sessiondelete-${sessionId}`);
                session.addEventListener('click', (event) => {
                    swal({
                        title: `Would you like to delete Session ?`,
                        text: ` ${roomName}`,
                        icon: "warning",
                        buttons: ["No", "Yes"],
                        dangerMode: true,
                    }).then((value) => {
                        if (value) {
                            const el = document.querySelector(`[data-session-id="${sessionMap.get(roomName)}"]`);
                            if (el)
                                el.remove();

                            deleteSession(authUser, roomName, entry)
                            const optList = document.getElementById("rcNameOptions");
                            if (optList)
                                optList.value = "Public";

                            setTimeout(() => {
                                document.getElementById(sessionMap.get('Public')).scrollIntoView({
                                    behavior: "smooth",
                                    block: 'center',
                                    inline: 'center',
                                });
                            }, 400)
                        } else {
                            console.log("User chose No!");
                        }
                    });

                });


            }

            const swapSessionCard = function (currentSession, newSession, targetName) {

                const preSession = document.getElementById(currentSession);
                if (preSession) {
                    preSession.style.border = ".3em solid white";
                    preSession.style.boxShadow = "2px 4px 8px black";
                }

                const curSession = document.getElementById(newSession);
                if (curSession) {
                    curSession.style.border = ".3em solid black";
                    curSession.style.boxShadow = "2px 4px 8px white"
                }


                document.getElementById('sessionLabel').innerText = String(targetName).split(`@`, 1).toString();;

                const elementId = 'message-' + newSession;
                const sessionCardElement = document.getElementById(elementId);
                sessionCardElement.innerText = "";
            }

            const updateSessionCard = function (sessionId, lastestMessage) {

                const elementId = 'message-' + sessionId;
                const sessionCardElement = document.getElementById(elementId);

                sessionCardElement.innerHTML = lastestMessage;

            }




            const createNewMessage = function (sender, message, messageId, now) {
                now = now || new Date().toLocaleTimeString('en-US');
                messageId = messageId || new Date().toLocaleTimeString('en-US');

				const entry = document.createElement('li');
				
                entry.classList.add("d-flex", "mx-2", 'py-2');
                entry.id = `messageId-${messageId}`;
                if (sender === "Public") {
                    entry.innerHTML = message;
                    entry.classList.add("text-center");
                    entry.classList.add("system-message");
                    entry.classList.add("float-none");
				} else if (sender.toLowerCase() === authUser.toLowerCase()) {
                    entry.classList.add("justify-content-start", "gap-2", "rounded")
                    entry.innerHTML =
                        `<fluent-card class="message left border border-success flex-row-reverse">
                                <div class="card-header p-3 d-flex align-items-center justify-content-between">
                                    <div class="d-flex align-items-between">
                                        <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-1.webp" alt="avatar"
                                                class="rounded-circle shadow-1-strong" width="48"/>
                                    </div>
                                </div>
                                <div class="card-body gap-1">
                                    <p class="mb-2 limited-text shadow-sm rounded form-control rounded px-2">
                                        ${message}
                                    </p>
                                    <p class="text-muted small mb-0"><i class="fa fa-clock"></i> ${now}</p>
									<p class="fw-bold mb-0 ms-3 fs-6 text-success">${sender.toLowerCase()}</p>
                                </div>
                            </div>
                        </fluent-card>`
                } else {
                entry.classList.add("justify-content-end", "gap-2", "rounded")
                    entry.innerHTML =
                    `<fluent-card class="message right border border-primary flex-row" >
                            <div class="card-header p-3 d-flex align-items-center justify-content-between">
                                <div class="d-flex align-items-between">
                                    <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-4.webp" alt="avatar"
                                            class="rounded-circle shadow-1-strong" width="48"/>
                                    
                                </div>
                            </div>
                            <div class="card-body">
                               <p class="mb-2 limited-text shadow-sm rounded form-control gap-4 rounded px-2">
                                    ${message}
                                </p>
                               <p class="text-muted small mb-0"><i class="fa fa-clock"></i> ${now}</p>
							   <p class="fw-bold mb-0 ms-3 fs-6 text-success">${sender.toLowerCase()}</p>
                            </div>
                        </div>
                    </fluent-card>`

                }

                return entry;
            }

            const addNewMessageToScreen = function (messageEntry) {
               
                const messageBoxElement = document.getElementById('messages');
                messageBoxElement.appendChild(messageEntry);
                messageBoxElement.scrollTop = messageBoxElement.scrollHeight;

                //  Clear text box and reset focus for next comment.
                messageInput.value = '';
                messageInput.focus();
              
            }

            const createNewMessageStatus = function (messageId, messageStatus) {
                const messageStatusEntry = document.createElement('div');
                messageStatusEntry.innerHTML =
                    `<div class="message-avatar pull-right" id="${messageId}-Status">
                                                    ${messageStatus}
                                                </div>`;
                return messageStatusEntry;
            }

            const updateMessageStatus = function (messageId, messageStatus, sequenceId) {
                const statusElement = document.getElementById(messageId + '-Status');
                const messageElement = document.getElementById('messageId-' + messageId);
                statusElement.innerText = messageStatus;
                statusElement.id = sequenceId + '-Status';
                messageElement.id = 'messageId-' + sequenceId;
            }

            const messageInput = document.getElementById('message');
            messageInput.focus();

            const addHistoryMessage = function (historyMessage, connection) {
                const messageBoxElement = document.getElementById('messages');
                messageBoxElement.innerHTML = ``;
                messageBoxElement.innerText = ``;

                let seqId = 0;
                historyMessage.forEach(element => {
                    let cnt = element.sequenceId || seqId;
                    let webTime = element.sendTime.substring(11, 19);

                    let messageEntry = createNewMessage(element.senderName,
                        element.messageContent, cnt, webTime);

                    if (currentSession != 'Public' && element.senderName != authUser) {
                        if (element.messageStatus != 'Read') {
                            connection.invoke('sendUserResponse', currentSession, cnt, element
                                .senderName, 'Read');
                        }
                    } else if (currentSession != 'Public' && element.senderName === authUser) {
                        const messageStatusEntry = createNewMessageStatus(cnt, element.messageStatus);
                        messageEntry.appendChild(messageStatusEntry);
                    }
                    
                    addNewMessageToScreen(messageEntry);
                    seqId = seqId + 1;    

                    
                    
                });
            }

            const changeSession = async function (targetName, connection) {
                if (targetName === authUser) {
                    alert('You cannot create a session with yourself!');
                    return;
                }

                //  Check if the session exists in local.
                var sessionId;

                if (sessionMap.has(targetName)) {
                    sessionId = sessionMap.get(targetName);
                } else {
                    sessionId = await connection.invoke('getOrCreateSession', targetName);
                    sessionMap.set(targetName, sessionId);

                    //  Add the sessionCard to the left side of the screen
                    addNewSessionCard(targetName, sessionId, '');
                }

                swapSessionCard(currentSession, sessionId, targetName);

                currentSession = sessionId;
                currentUser = targetName;
                const historyMessage = await connection.invoke('loadMessages', sessionId);;

                //  Add the history message to the screen
                addHistoryMessage(historyMessage, connection);
            }

            const sendUserMessage = async function (connection) {
                
                messageInput.value = htmlToMarkdown(messageInput.innerHTML);
                if (!messageInput.value) {
                    return;    
                }
                const messageId = generateGuid();
                const sessionId = currentSession;
                const targetName = currentUser;

                //  Create the message in the window.
                const messageText = messageInput.value;
                messageInput.value = '';
                messageInput.innerHTML = ''; 

                //  Currently we pull all messages from the server
                await changeSession(currentUser, connection);

                //  Create the message in the room.
                const messageEntry = createNewMessage(authUser, messageText, messageId, '');
                const messageStatusEntry = createNewMessageStatus(messageId, 'Sending');
                messageEntry.appendChild(messageStatusEntry);
                addNewMessageToScreen(messageEntry);
                const messageBoxElement = document.getElementById('messages');
                messageBoxElement.scrollTop = messageBoxElement.scrollHeight;

                //  Call the sendUserMessage method on the hub.
                const sequenceId = currentSession === 'Public'
                    ? await connection.invoke('broadcastMessage', messageText)
                    : await connection.invoke('sendUserMessage', sessionId, targetName, messageText);

                currentSequenceMessage = sequenceId; 
                updateMessageStatus(messageId, 'Sent', sequenceId);
                updateSessionCard(sessionId, messageText);
            }

            const deleteSession = function (userName, roomName, element) {
                if (roomName === 'Public') {
                    SnackBar({ status: 'error', icon: 'danger', speed: "0.5s", positon: "tc", message: "You can't delete Public room ever!" });
                } else {
                    if (roomName !== null) {
                        connection.invoke('deleteUserSession', userName, roomName);
                        element.remove();
                        sessionMap.delete(roomName);
                        SnackBar({ status: 'info', timeout: '2400', icon: 'info', message: ` (${roomName}) room was deleted successfully !` })

                    }
                }
                changeSession('Public', connection);
                event.preventDefault();
                event.stopPropagation();
            }

            const bindConnectionMessage = function (connection) {
                const displayUnreadMessage = function (sessionId, sender, messageContent) {
                    var time = new Date().toLocaleTimeString();
                    if (sessionMap.has(sender) || sessionId === 'Public') {
                    updateSessionCard(sessionId, "<p class='limited-text'>New Message...\t" + time + "\n" + messageContent + "</p>");
                        setTimeout(() => { 
                            statusBar.innerHTML = "";
                        }, 5000)
                        statusBar.innerHTML =  "<article class='w-25 p-2'>New Message..." + '\t' + time + "\<p>" + htmlToMarkdown(messageContent) + "</p><br>" +
                        "Room name: " + getByValue(sessionMap, sessionId) + "</article>";
                } else {
                        sessionMap.set(sender, sessionId);
                        addNewSessionCard(sender, sessionId, sender + ': ' + messageContent);
                    }

                    if (window.Notification && Notification.permission !== 'denied') {
                        Notification.requestPermission(function (status) {
                            let n = new Notification('You have a new nessage from' + sender, {
                                body: messageContent
                            });
                        });
                    }
                }

                const showLoginUsers = function (userNameList) {
                    const list = document.getElementById('principalList');

                    if (!list)
                        return;
                    while (list.lastChild) {
                        list.lastChild.remove();
                    }

                    // parent meymyself and I. 
                    const myself = document.createElement('li');

                    myself.innerHTML = `<div class="shadow-sm border-1 p-2"><h4>Me</h4><div>${authUser.split("@", 1).toString()}</div><hr/></div>`;
                    list.append(myself);
                    myself.classList.add('disabled');

                    fetch("/users/all", {
                        method: "GET", 
                        mode: "cors", 
                        credentials: "include", 
                        headers: {
                            "Content-type": "application/json"
                        },
                        body: null
                    }).then((us) => {
                        if (!us.ok) {
                            throw new Error("Can't load members; pleade refresh page");
                        }
                        return us.json();
                    }).then((data) => {

                        data.forEach(u => {
                            const login = document.createElement('li');
                            login.classList.add('dropdown-item');
                            login.innerHTML =
                                `<div class="align-items-center">
                                            <a href="#">
                                                <div class="card-body d-flex">
                                                        <img src="https://mdbcdn.b-cdn.net/img/Photos/Avatars/avatar-${getRandomInt(1, 16)}.webp" alt="avatar"
                                                                    class="rounded-circle d-flex align-self-start shadow-1-strong" width="40">
                                                    <div class="m-2">${u.split("@", 1).toString()}</div>
                                                </div>
                                            </a>
                                        </div>`
                            if (userNameList.includes(u)) {
                                login.classList.add('active-list-item');
                            }
                            if (authUser !== u) {
                                login.addEventListener('click', function (event) {
                                    event.preventDefault();
                                    changeSession(u, connection);
                                    setTimeout(() => {
                                        document.getElementById(sessionMap.get(this.value)).scrollIntoView({
                                            behavior: "smooth",
                                            block: 'center',
                                            inline: 'center'
                                        });
                                    }, 400);
                                    return false;
                                });
                                list.appendChild(login);
                            }
                        });

                    }).catch((e) => console.dir(e));
                };
                //  Add the message to the screen
                const displayUserMessage = function (sessionId, sequenceId, sender, messageContent) {
                    if (currentSession != sessionId) {
                        displayUnreadMessage(sessionId, sender, messageContent);
                        return;
                    }

                    if (currentSession != 'Public') {
                        connection.invoke('sendUserResponse', sessionId, sequenceId, sender, 'Read');
                    }

                    const messageEntry = createNewMessage(sender, messageContent, sequenceId, '');
                    addNewMessageToScreen(messageEntry);
                };

                //  Change the status text under the message
                const displayResponseMessage = function (sessionId, sequenceId, messageStatus) {
                    if (sessionId != currentSession) {
                        return;
                    }

                    // if it's a private message this user doesn't own the sequence so sent to originator
                    if (document.getElementById(sequenceId + '-Status')) {

                        const now = new Date();
                        const entry = document.getElementById(sequenceId + '-Status');
                        entry.innerText = messageStatus;
                        console.log('messageId: ' + sessionId + '\nStatus: ' + messageStatus +
                            '\nLocal Time: ' + now.toLocaleTimeString());

                    };


                }


                //  Log all sessions
                const updateSessions = function (sessions) {
                    console.log(sessions);

                    sessions.forEach(element => {

                        // add only new rooms so view isn't affected.
                        if (!sessionMap.has(element.key)) {
                            sessionMap.set(element.key, element.value.sessionId);
                            addNewSessionCard(element.key, element.value.sessionId, '');
                            var opt = document.createElement('option');
                            opt.value = element.key;
                            opt.dataset.sessionId = element.value.sessionId;
                            opt.innerHTML = element.key;
                            rcNameOptions.appendChild(opt);
                        }
                    });


                }
                const deletedRoom = function (session) {
                    // if we get here the user has multiple webbrowser sessions so remove session.

                    SnackBar({ message: "A room has been removed by you from another session", status: "success", icon: 'minus', timeout: 4000, dismissible: true });

                }


                const sendPrivateMessage = function (sessionId, message, emailText, sender) {

                    if (currentSession == sessionId)
                        return; 

                    var snack = SnackBar({
                        message: 'View Message',
                        dismissable: true,
                        timeout: 40000,
                        icon: 'plus',
                        actions: [
                            {
                                text: message,
                                dismiss: true,
                                function: function () {
                                    changeSession(sender, connection);
                                    
                                    setTimeout(() => {

                                        document.getElementById(sessionId).scrollIntoView({
                                            behavior: "smooth",
                                            block: 'center',
                                            inline: 'center',
                                        });

                                    }, 1000)
                                    

                                }
                            }
                        ]
                    });


                }
                const sendNotifyUser = function (message) {
                    SnackBar({ message: message, status: "success", icon: "plus", timeout: 5000, dismissible: true });
                }
                //  Create a function that the hub can call to broadcast messages.
                connection.on('sendNotify', sendNotifyUser);
                connection.on('displayResponseMessage', displayResponseMessage);
                connection.on('displayUserMessage', displayUserMessage);
                connection.on('updateSessions', updateSessions);
                connection.on('showLoginUsers', showLoginUsers)
                connection.on('deletedRoom', deletedRoom);
                connection.on('sendPrivateMessage', sendPrivateMessage);
                connection.onclose(onConnectionError);
            }


            function onConnected(connection) {
                Notification.requestPermission(function (status) {
                    if (status === 'granted') {
						let n = new Notification('Dear ' +  authUser.split("@", 1).toString()  +
                            ': \nYour new messages will be displayed here');
                    }
                });
                addNewSessionCard('Public', 'Public', '');
                changeSession('Public', connection);

                document.getElementById('userName').innerText = String(authUser).split(`@`, 1).toString();

                document.getElementById('sendmessage').addEventListener('click', (event) => sendUserMessage(connection));

                document.getElementById('message').addEventListener('keypress', function (event) {
                    if (event.key == 'Enter') {
                        event.preventDefault();
                        document.getElementById('sendmessage').click();
                        return false;
                    }
                });

                document.getElementById('receiverName').addEventListener('keypress', function (event) {
                    if (event.key == 'Enter') {
                        event.preventDefault();
                        changeSession(this.value, connection);
                        this.value = '';
                        return false;
                    }
                });

                document.getElementById('rcNameOptions').addEventListener('change', function (event) {

                    event.preventDefault();
                    setTimeout(() => {
                        document.getElementById(sessionMap.get(this.value)).scrollIntoView({
                            behavior: "smooth",
                            block: 'center',
                            inline: 'center'
                        });
                    }, 400);
                    // default run below
                    changeSession(this.value, connection)
                    messageInput.value = '';
                    messageInput.focus();
                    return false;
                });
            }
            
            async function SendToAzure(frmData){
                 
                try {
                    const response = await fetch('MessageBoard/UploadFile', {
                        method: 'POST',
                        body: frmData
                    });

                    if (response.ok) {
                        const data = await response.json();
                        console.log(data);
                        return data;
                    } else {
                        console.error("Error: response not ok");
                        return undefined;
                    }
                } catch (error) {
                    console.error("Error", error);
                    return undefined;
                }
                 
            }

            function OffSetandScroll(elementId) {

                const element = document.getElementById(elementId);
                const elementRect = element.getBoundingClientRect();
                const absoluteElementTop = elementRect.top + window.pageYOffset;
                const middle = absoluteElementTop - (window.innerHeight / 2);
                window.scrollTo(0, middle);

            }
            function onConnectionError(error) {
                if (error && error.message) {
                    console.error(error);
                }

                const modal = document.getElementById('myModal');
                modal.classList.add('in');
                modal.style = 'display: block;';
                const msg = document.getElementById('msgModal');
                msg.innerHTML = "Connection failure....";
            }

            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/message")
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information)
                .build();

            connection.serverTimeoutInMilliseconds = 120000;


            bindConnectionMessage(connection);

            connection.start()
                .then(function () {
                    onConnected(connection);
                    console.log("AzureChat has STARTED " + new Date().toLocaleString());
                })
                .catch(function (error) {
                    console.dir(error);
                    onConnectionError(error);
                });

            connection.onclose(function () {
                setTimeout(function () {
                    connection.start().then(t => onConnected(connection)).catch(err => onConnectionError(err));
                }, 5000);
                console.log("chat has been disconnected..  " + new Date().toLocaleString());
            });
        });
		})(); 