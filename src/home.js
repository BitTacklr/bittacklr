function toggleMenu()
{
    var hamburger = document.getElementById("largemenuhamburger");
    if ( hamburger == undefined) return;
    var menu = document.getElementById("largemenu");
    if ( menu == undefined) return;

    if ( hamburger.className !== "" )
    {
        hamburger.className = "";
        menu.className = "";
    }
    else 
    {
        hamburger.className = "open";
        menu.className = "open";
    }
}