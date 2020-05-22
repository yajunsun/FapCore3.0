﻿"use strict";
var connection = new signalR.HubConnectionBuilder().withUrl("/onlineHub").build();

//上线通知
connection.on("Online", function (onlineUser) {    
    var fid = onlineUser.empUid;
    $(".profile-partner").find("[data-id='" + fid + "']").find(".user-status").removeClass("status-offline").addClass("status-online");
    //添加聊天按钮
    $(".profile-partner").find("[data-id='" + fid + "']").append(`<div class="tools action-buttons">
            <a href="javascript:void(0)" data-chat="`+ fid + `" data-emp="` + onlineUser.empName +`" class="blue">
                <i class="ace-icon fa fa-comments bigger-125"></i>
            </a>
        </div>`);
});
connection.on("Offline", function (onlineUser) {
    var fid = onlineUser.empUid;
    $(".profile-partner").find("[data-id='" + fid + "']").find(".user-status").removeClass("status-online").addClass("status-offline");
    //移除聊天按钮
    $(".profile-partner").find("[data-id='" + fid + "']").find(".tools.action-buttons").remove();
});
//伙伴申请通知
connection.on("PartnerApplyNotifications", function (applier) {
    $.msg(applier + "申请加您为伙伴");
});
//伙伴审核通知
connection.on("PartnerCheckNotifications", function (applier) {
    if (applier.reqState === "Agree") {
        $.msg(applier.empName + "同意了您的伙伴申请");
        var content = `<div class="profile-activity clearfix" data-id="`+applier.fid+`">
        <div>
            <img class="pull-left" alt="头像" src="~/Component/Photo/`+ applier.empPhoto + `">
                <span class="user bolder" href="#">                
                <span class="user-status status-online" title="online"></span>
                    `+ applier.empName + `
            </span>

            <div class="time">
                `+ applier.deptUidMC + `
            </div>
        </div>
        <div class="tools action-buttons">
            <a href="javascript:void(0)" data-chat="`+ applier.fid + `" data-emp="` + applie.empName +`" class="blue">
                <i class="ace-icon fa fa-comments bigger-125"></i>
            </a>
        </div>
    </div>`;
        $(".profile-partner").append(content);
    } else {
        $.msg(applier.empName + "拒绝了您的伙伴申请");
    }
});
connection.start().then(function () {
    //document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});
$(".profile-partner").off(ace.click_event, "[data-chat]").on(ace.click_event, "[data-chat]", function () {
    var $this = $(this);
    //伙伴名称
    var empName = $this.data("emp");
    //伙伴ID
    var user = $this.data("chat");
    var chatDialog = bootbox.dialog({
        title: 'To: ' + empName,
        message: 'loading',
        footer: false
    });
    chatDialog.init(function () {
        $.get(basePath + "/Content/chat/chat.html", function (ev) {
            chatDialog.find('.bootbox-body').html(ev);
            //标记和谁聊天框
            chatDialog.find(".active-chat.fap-im").data("chatuser", user);
            //更新聊天内容（从数据库读取）
            //临时读取未读内容
            var $chatUser= $this.closest('.profile-activity');
            var content = $chatUser .data("noreader");
            if (content) {
                var chatContent = ` <div class="bubble you">
                        `+ content + `
<br/><small>`+ moment().locale('zh-cn').format('YYYY-MM-DD HH:mm:ss') + `</small>
                    </div>`;
                chatDialog.find(".active-chat").append(chatContent);
                $chatUser.data("noreader", "");
                $chatUser.find(".arrowed").html('');
            }
            chatDialog.find("#sendButton").on(ace.click_event, function (event) {                
                var message = chatDialog.find("#userInputMessage").val();
                if (message === "") {
                    return;
                }
                connection.invoke("SendMessage", user, message).catch(function (err) {
                    return console.error(err.toString());
                });
                var chatContent = ` <div class="bubble me">
                        `+ message +`
<br/><small>`+ moment().locale('zh-cn').format('YYYY-MM-DD HH:mm:ss') +`</small>
                    </div>`;
                chatDialog.find(".active-chat").append(chatContent);
                chatDialog.find("#userInputMessage").val("");
                event.preventDefault();
            });
        });
    });
});
//userName.
connection.on("ReceiveMessage", function (fromUserName,fromUserid, message) {
    var msg = message.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var dialogChat = $(".active-chat.fap-im");
    if (dialogChat.data("chatuser") === fromUserid) {
        var chatContent = ` <div class="bubble you">
                        `+ msg + `
<br/><small>`+ moment().locale('zh-cn').format('YYYY-MM-DD HH:mm:ss') + `</small>
                    </div>`;
        dialogChat.append(chatContent);
    } else {
        var $chatUser = $(".profile-partner").find("[data-id='" + fromUserid + "']");
        if ($chatUser !== undefined) {
            var $arrowed = $chatUser.find(".arrowed");
            if ($arrowed.get(0) != undefined) {
                var c = parseInt($arrowed.html()) + 1;
                $arrowed.html('+' + c);
            } else {
                $chatUser.find(".user").append(' <span class="label label-purple arrowed">1</span>');
            }            
        }
        //临时存储未读内容
        var content = $chatUser.data("noreader");
        if (content === undefined) {
            $chatUser.data("noreader", msg);
        } else {
            $chatUser.data("noreader", content + ">>>" + msg);
        }
        //存入数据库
        $.msg(fromUserName + ":" + msg);
    }
});