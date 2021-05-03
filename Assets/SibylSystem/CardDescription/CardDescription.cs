﻿using UnityEngine;
using System;
using System.Collections.Generic;
using YGOSharp.OCGWrapper.Enums;

public class CardDescription : Servant
{
    cardPicLoader picLoader;
    UIDragResize resizer;
    UITexture underSprite;
    UITexture picSprite;
    UISprite lineSprite;
    public UITextList description;
    GameObject line;
    UIWidget cardShowerWidget;
    UIPanel monitor;
    UIDeckPanel deckPanel;

    cardPicLoader[] quickCards = new cardPicLoader[300];

    public override void initialize()
    {
        gameObject = create
            (
            Program.I().new_ui_cardDescription,
            Program.camera_main_2d.ScreenToWorldPoint(new Vector3(-256, Screen.height /2, 600)),
            new Vector3(0, 0, 0),
            true,
            Program.ui_back_ground_2d
            );
        picLoader = gameObject.AddComponent<cardPicLoader>();
        picLoader.code = 0;
        picLoader.uiTexture = UIHelper.getByName<UITexture>(gameObject, "pic_");
        picLoader.loaded_code = -1;
        resizer = UIHelper.getByName<UIDragResize>(gameObject, "resizer");
        underSprite = UIHelper.getByName<UITexture>(gameObject, "under_");
        description = UIHelper.getByName<UITextList>(gameObject, "description_");
        cardShowerWidget = UIHelper.getByName<UIWidget>(gameObject, "card_shower");
        monitor = UIHelper.getByName<UIPanel>(gameObject, "monitor");
        deckPanel = gameObject.GetComponentInChildren<UIDeckPanel>();
        line = UIHelper.getByName(gameObject, "line");
        UIHelper.registEvent(gameObject,"pre_", onPre);
        UIHelper.registEvent(gameObject, "next_", onNext); 
        UIHelper.registEvent(gameObject, "big_", onb);
        UIHelper.registEvent(gameObject, "small_", ons);
        UIHelper.registEvent(gameObject, "wiki_", onwiki);
        UIHelper.registEvent(gameObject, "reset_", onReset);
        picSprite = UIHelper.getByName<UITexture>(gameObject, "pic_");
        lineSprite = UIHelper.getByName<UISprite>(gameObject, "line");
        try
        {
            description.textLabel.fontSize = int.Parse(Config.Get("fontSize","24"));
        }
        catch (System.Exception e)
        {
        }
        read();
        myGraveStr = InterString.Get("我方墓地：");
        myExtraStr = InterString.Get("我方额外：");
        myBanishedStr = InterString.Get("我方除外：");
        opGraveStr = InterString.Get("[8888FF]对方墓地：[-]");
        opExtraStr = InterString.Get("[8888FF]对方额外：[-]");
        opBanishedStr = InterString.Get("[8888FF]对方除外：[-]");
        for (int i = 0; i < quickCards.Length; i++)
        {
            quickCards[i] = deckPanel.createCard();
            quickCards[i].relayer(i);
        }
        monitor.gameObject.SetActive(false);
    }

    public override void ES_RMS(string hashCode, List<messageSystemValue> result)
    {
        base.ES_RMS(hashCode, result);
        if (hashCode == "RMSshow_onWiki")
        {
            if (result[0].value == "yes")
            {
                if (Application.systemLanguage == SystemLanguage.ChineseSimplified || Application.systemLanguage == SystemLanguage.ChineseTraditional || Application.systemLanguage == SystemLanguage.Chinese)
                {
                    #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
                    InAppBrowser.OpenURL("https://www.ourocg.cn/S.aspx?key=" + currentCard.Id.ToString());
                    #else
                    Application.OpenURL("https://www.ourocg.cn/S.aspx?key=" + currentCard.Id.ToString());
                    #endif
                }
                else
                {
                    #if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IPHONE)
                    InAppBrowser.OpenURL("https://yugipedia.com/wiki/" + currentCard.Id.ToString());
                    #else
                    Application.OpenURL("https://yugipedia.com/wiki/" + currentCard.Id.ToString());
                    #endif
                }
            }
        }
    }

    public float width = 0;
    public float cHeight = 0;

    void onb()
    {
        description.textLabel.fontSize += 1;
        description.Rebuild();
        Config.Set("fontSize", description.textLabel.fontSize.ToString());
    }

    void ons()
    {
        description.textLabel.fontSize -= 1;
        description.Rebuild();
        Config.Set("fontSize", description.textLabel.fontSize.ToString());
    }

    void onPre()
    {
        current--;
        loadData();
    }

    void onNext()
    {
        current++;
        loadData();
    }

    void onwiki()
    {
        RMSshow_yesOrNo("RMSshow_onWiki", InterString.Get("是否在线查询卡片调整？"), new messageSystemValue { hint = "yes", value = "yes" }, new messageSystemValue { hint = "no", value = "no" });
    }

    void onReset()
    {
        if (Screen.width >= 2300)
        {
            underSprite.width = 420;
            picSprite.height = 525;
            description.textLabel.fontSize = 30;
        }
        else if (Screen.width >= 1900)
        {
            underSprite.width = 360;
            picSprite.height = 445;
            description.textLabel.fontSize = 25;
        }
        else if (Screen.width >= 1500)
        {
            underSprite.width = 300;
            picSprite.height = 365;
            description.textLabel.fontSize = 20;
        }
        else
        {
            underSprite.width = 240;
            picSprite.height = 285;
            description.textLabel.fontSize = 15;
        }
        Program.PrintToChat(InterString.Get("已重置调整，如不合适请自行调整"));
    }

    public override void applyHideArrangement()
    {
        if (gameObject != null)
        {
            underSprite.height = Screen.height + 4;
            iTween.MoveTo(gameObject, Program.camera_main_2d.ScreenToWorldPoint(new Vector3(-underSprite.width-20, Screen.height / 2, 0)), 1.2f);
            setTitle("");
            resizer.gameObject.GetComponent<BoxCollider>().enabled = false;
        }
    }

    public override void applyShowArrangement()
    {
        if (gameObject != null)
        {
            underSprite.height = Screen.height + 4;
            iTween.MoveTo(gameObject, Program.camera_main_2d.ScreenToWorldPoint(new Vector3(-2, Screen.height / 2, 0)), 1.2f);
            resizer.gameObject.GetComponent<BoxCollider>().enabled = true;
        }
    }

    public void read()
    {
        try
        {
            var ca = int.Parse(Config.Get("CA", "230"));
            var cb = int.Parse(Config.Get("CB", "270"));
            if (cb > Screen.height)
            {
                // some dumb ass repack the program and set the pic size so large that small screen users can't realize there is card description under it.
                cb = Screen.height / 2;
                ca = (int)(cb * 0.68) + 50;
            }
            underSprite.width = ca;
            picSprite.height =  cb; 
        }
        catch (System.Exception e)
        {
        }
    }

    public void save()
    {
        Config.Set("CA", underSprite.width.ToString());
        Config.Set("CB", picSprite.height.ToString());
        Config.Set("fontSize", description.textLabel.fontSize.ToString());
    }

    class data
    {
        public YGOSharp.Card card;
        public Texture2D def;
        public string tail = "";
    }

    int current = 0;

    List<data> datas = new List<data>();

    void loadData()
    {
        if (current < 0)
        {
            current = 0;
        }
        if (current > datas.Count - 1)
        {
            current = datas.Count - 1;
        }
        if (datas.Count == 0)
        {
            return;
        }
        data d = datas[current];
        apply(d.card, d.def, d.tail);
    }

    YGOSharp.Card currentCard = null;

    public bool ifShowingThisCard(YGOSharp.Card card)   
    {
        return currentCard == card;
    }

    private void apply(YGOSharp.Card card, Texture2D def, string tail)
    {
        if (card == null)
        {
            return;
        }
        string smallstr = "";
        if (card.Id != 0)
        {
            smallstr = GameStringHelper.getName(card) + GameStringHelper.getSmall(card);
            smallstr += "\n";
        }
        if (tail == "")
        {
            description.Clear();
            description.Add(smallstr + card.Desc);
        }
        else
        {
            description.Clear();
            description.Add(smallstr + "[FFD700]" + tail + "[-]" + card.Desc);
        }
        picLoader.code = card.Id;
        picLoader.defaults = def;
        picLoader.loaded_code = -1;
        currentCard = card;
        shiftCardShower(true);
        Program.go(50, () => { shiftCardShower(true); });
    }

    public void shiftCardShower(bool show)
    {
        if (show)
        {
            cardShowerWidget.alpha = 1f;
        }
        else
        {
            cardShowerWidget.alpha = 0f;
        }
        if (!show)
        {
            if (monitor.gameObject.activeInHierarchy == false)
            {
                monitor.gameObject.SetActive(true);
                realizeMonitor();
            }
        }
        else
        {
            monitor.gameObject.SetActive(false);
        }
    }

    string myGraveStr = "";
    string myExtraStr = "";
    string myBanishedStr = "";
    string opGraveStr = "";
    string opExtraStr = "";
    string opBanishedStr = "";


    public void realizeMonitor()
    {
        if (monitor.gameObject.activeInHierarchy)
        {
            List<gameCard> myGrave = new List<gameCard>();
            List<gameCard> myExtra = new List<gameCard>();
            List<gameCard> myBanished = new List<gameCard>();
            List<gameCard> opGrave = new List<gameCard>();
            List<gameCard> opExtra = new List<gameCard>();
            List<gameCard> opBanished = new List<gameCard>();
            for (int i = 0; i < Program.I().ocgcore.cards.Count; i++)
            {
                var curCard = Program.I().ocgcore.cards[i];
                int code = curCard.get_data().Id;
                var gps = curCard.p;
                if (code > 0)
                {
                    if (gps.controller == 0)
                    {
                        if ((gps.location & (UInt32)CardLocation.Grave) > 0)
                        {
                            myGrave.Add(curCard);
                        }
                        if ((gps.location & (UInt32)CardLocation.Removed) > 0)
                        {
                            myBanished.Add(curCard);
                        }
                        if ((gps.location & (UInt32)CardLocation.Extra) > 0)
                        {
                            myExtra.Add(curCard);
                        }
                    }
                    else
                    {
                        if ((gps.location & (UInt32)CardLocation.Grave) > 0)
                        {
                            opGrave.Add(curCard);
                        }
                        if ((gps.location & (UInt32)CardLocation.Removed) > 0)
                        {
                            opBanished.Add(curCard);
                        }
                        if ((gps.location & (UInt32)CardLocation.Extra) > 0)
                        {
                            opExtra.Add(curCard);
                        }
                    }
                }
            }
            currentHeight = 0;
            currentLabelIndex = 0;
            currentCardIndex = 0;
            handleMonitorArea(opGrave, opGraveStr);
            handleMonitorArea(opBanished, opBanishedStr);
            handleMonitorArea(opExtra, opExtraStr);
            handleMonitorArea(myGrave, myGraveStr);
            handleMonitorArea(myBanished, myBanishedStr);
            handleMonitorArea(myExtra, myExtraStr);
            while (currentLabelIndex < 6)
            {
                deckPanel.labs[currentLabelIndex].gameObject.SetActive(false);
                currentLabelIndex++;
            }
            while (currentCardIndex < 300)
            {
                quickCards[currentCardIndex].clear();
                currentCardIndex++;
            }
        }
    }

    float currentHeight = 0;
    int currentLabelIndex = 0;
    int currentCardIndex = 0;
    int eachLine = 0;
    float monitorHeight = 0;

    void handleMonitorArea(List<gameCard> list,string hint)
    {
        if (list.Count > 0)
        {
            deckPanel.labs[currentLabelIndex].gameObject.SetActive(true);
            deckPanel.labs[currentLabelIndex].text = hint;
            deckPanel.labs[currentLabelIndex].width = (int)(monitor.width - 12);
            deckPanel.labs[currentLabelIndex].transform.localPosition = new Vector3(monitor.width / 2, (monitor.height - 8) / 2 - 12 - currentHeight, 0);
            currentLabelIndex++;
            currentHeight += 24;
            float beginX = 6 + 22;
            float beginY = monitor.height / 2 - currentHeight - 36;
            eachLine = (int)((monitor.width - 12f) / 44f);
            for (int i = 0; i < list.Count; i++)
            {
                var gp = UIHelper.get_hang_lie(i, eachLine);
                quickCards[currentCardIndex].reCode(list[i].get_data().Id);
                quickCards[currentCardIndex].transform.localPosition = new Vector3(beginX + 44 * gp.y, beginY - 60 * gp.x, 0);
                currentCardIndex++;
            }
            int hangshu = list.Count / eachLine;
            int yushu = list.Count % eachLine;
            if (yushu > 0)
            {
                hangshu++;
            }
            currentHeight += 60 * hangshu;
        }
    }
    
    public void onResized()
    {
        if (monitor.gameObject.activeInHierarchy)
        {
            int newEach = (int)((monitor.width - 12f) / 44f);
            if (newEach != eachLine || monitorHeight != monitor.height)
            {
                monitorHeight = monitor.height;
                eachLine = newEach;
                realizeMonitor();
            }
        }
        if (underSprite.width < 240) { underSprite.width = 240; }
        if (picSprite.height < 285) { picSprite.height = 285; }
    }

    public void setData(YGOSharp.Card card, Texture2D def, string tail = "",bool force=false)
    {
        if (cardShowerWidget.alpha == 0&&force==false)
        {
            return;
        }
        if (card == null)
        {
            return;
        }
        if (card.Id == 0)
        {
            apply(card,def,tail);
            return;
        }
        if (datas.Count > 0)
        {
            if (datas[datas.Count - 1].card.Id == card.Id)
            {
                datas[datas.Count - 1] = new data
                {
                    card = card,
                    def = def,
                    tail = tail
                };
                if (datas.Count > 300)
                {
                    datas.RemoveAt(0);
                }
                current = datas.Count - 1;
                loadData();
                return;
            }
        }
        datas.Add(new data
        {
            card = card,
            def = def,
            tail = tail
        });
        if (datas.Count>300)    
        {
            datas.RemoveAt(0);
        }
        current = datas.Count - 1;
        loadData();
    }

    public void setTitle(string title)
    {
        UIHelper.trySetLableText(gameObject,"title_",title);
    }

    List<string> Logs = new List<string>();

    public void mLog(string result)
    {
        Logs.Add(result);
        string all = "";
        for (int i = 0; i < Logs.Count; i++)
        {
            if (i == Logs.Count - 1)
            {
                all += Logs[i].Replace("\0", "");
            }
            else
            {
                all += Logs[i].Replace("\0", "") + "\n";
            }
        }
        UIHelper.trySetLableTextList(UIHelper.getByName(gameObject, "chat_"), all);
        Program.go(8000, clearOneLog);
    }

    void clearOneLog()
    {
        if (Logs.Count>0)
        {
            Logs.RemoveAt(0);
            string all = "";
            foreach (var item in Logs)
            {
                all += item + "\n";
            }
            try
            {
                all = all.Substring(0, all.Length - 1);
            }
            catch (System.Exception e)
            {
            }
            UIHelper.trySetLableTextList(UIHelper.getByName(gameObject, "chat_"), all);
        }
        else
        {
            UIHelper.trySetLableTextList(UIHelper.getByName(gameObject, "chat_"), "");
        }

    }
    public void clearAllLog()
    {
        Program.notGo(clearOneLog);
        Logs.Clear();
        UIHelper.trySetLableTextList(UIHelper.getByName(gameObject, "chat_"), "");
    }
}
