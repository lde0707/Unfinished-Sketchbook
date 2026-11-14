mergeInto(LibraryManager.library, {
  Stilldrawn_SetResolution: function(w,h) {
    window.stilldrawnResolution={width:w,height:h};
    if(window.resizeStilldrawn)window.resizeStilldrawn();
    else {Module.canvas.width=w;Module.canvas.height=h;}
  },
  Stilldrawn_EditNote: function(value) {
    if(window.stilldrawnCloseNote)window.stilldrawnCloseNote();
    var input=document.createElement('textarea');input.id='stilldrawn-map-note';input.maxLength=160;
    input.value=UTF8ToString(value);input.setAttribute('aria-label','지도 메모');
    Object.assign(input.style,{position:'fixed',zIndex:1000,resize:'none',border:'none',outline:'none',background:'transparent',color:'#ddd5be',fontFamily:'serif',lineHeight:'1.6',padding:'10px',boxSizing:'border-box'});
    function place(){var host=document.fullscreenElement||document.body;if(input.parentElement!==host){host.appendChild(input);input.focus();}var r=Module.canvas.getBoundingClientRect(),s=Math.min(r.width/1920,r.height/1080);input.style.left=(r.left+(r.width-1920*s)/2+1270*s)+'px';input.style.top=(r.top+(r.height-1080*s)/2+345*s)+'px';input.style.width=(370*s)+'px';input.style.height=(220*s)+'px';input.style.fontSize=(24*s)+'px';input.style.padding=(10*s)+'px';}
    function send(method,value){if(window.unityInstance)window.unityInstance.SendMessage('World Prototype',method,value||'');}
    input.addEventListener('input',function(){send('SetMapNoteDraft',input.value);});
    input.addEventListener('keydown',function(e){e.stopPropagation();if(e.key==='Escape'){e.preventDefault();send('FinishMapNoteEditing','');}if((e.ctrlKey||e.metaKey)&&e.key==='Enter'){e.preventDefault();send('SaveMapNote','');}});
    input.addEventListener('keyup',function(e){e.stopPropagation();});
    (document.fullscreenElement||document.body).appendChild(input);place();input.focus();
    window.addEventListener('resize',place);document.addEventListener('fullscreenchange',place);
    window.stilldrawnCloseNote=function(){input.remove();window.removeEventListener('resize',place);document.removeEventListener('fullscreenchange',place);window.stilldrawnCloseNote=null;Module.canvas.focus();};
  },
  Stilldrawn_CloseNote: function(){if(window.stilldrawnCloseNote)window.stilldrawnCloseNote();}
});
