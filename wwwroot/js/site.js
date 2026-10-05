// Sidebar menüyü database'den yükle
function buildSidebarMenu() {
    $.ajax({
        url: '/SidebarMenu/GetMenu',
        type: 'GET',
        success: function(menuItems) {
            if (menuItems && menuItems.length > 0) {
                var sidebarHtml = '<ul>';
                menuItems.forEach(function(item) {
                    sidebarHtml += buildMenuItem(item);
                });
                sidebarHtml += '</ul>';
                $('#sidebar-menu').html(sidebarHtml);
                
                // Submenu event'lerini ekle
                initializeSubmenuEvents();
            } else {
                // Fallback: Statik menü
                buildStaticSidebarMenu();
            }
        },
        error: function(xhr, status, error) {
            buildStaticSidebarMenu();
        }
    });
}

// Tek menü öğesini oluştur
function buildMenuItem(item) {
    var hasSubmenu = item.submenus && item.submenus.length > 0;
    var submenuClass = hasSubmenu ? 'has-submenu' : '';
    var submenuIndicator = hasSubmenu ? '<i class="fas fa-chevron-right submenu-indicator"></i>' : '';
    
    var html = `<li class="nav-item ${submenuClass}">`;
    html += `<a class="nav-link" href="${item.url || '#'}">`;
    html += `<i class="${item.iconClass || 'fas fa-circle'}"></i>`;
    html += `<span>${item.title}</span>`;
    html += submenuIndicator;
    html += `</a>`;
    
    if (hasSubmenu) {
        html += `<ul class="submenu">`;
        item.submenus.forEach(function(subItem) {
            html += `<li><a href="${subItem.url || '#'}">`;
            if (subItem.iconClass) {
                html += `<i class="${subItem.iconClass}"></i>`;
            }
            html += `<span>${subItem.title}</span>`;
            html += `</a></li>`;
        });
        html += `</ul>`;
    }
    
    html += `</li>`;
    return html;
}

// Statik fallback menü
function buildStaticSidebarMenu() {
    var staticMenu = `
        <ul>
            <li class="nav-item">
                <a class="nav-link" href="/">
                    <i class="fas fa-home"></i>
                    <span>Ana Sayfa</span>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link" href="/Duyuru">
                    <i class="fas fa-bell"></i>
                    <span>Duyurular</span>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link" href="/PortalBolum">
                    <i class="fas fa-th-large"></i>
                    <span>Portal Bölümleri</span>
                </a>
            </li>
            <li class="nav-item">
                <a class="nav-link" href="/Reports">
                    <i class="fas fa-chart-bar"></i>
                    <span>Raporlar</span>
                </a>
            </li>
        </ul>
    `;
    $('#sidebar-menu').html(staticMenu);
}

// Submenu event'lerini başlat
function initializeSubmenuEvents() {
    // Hover event'leri
    $('.has-submenu').hover(
        function() {
            var submenu = $(this).find('.submenu');
            if (submenu.length > 0) {
                submenu.show();
                console.log('Submenu shown for:', $(this).find('a span').text());
            }
        },
        function() {
            var submenu = $(this).find('.submenu');
            if (submenu.length > 0) {
                submenu.hide();
                console.log('Submenu hidden for:', $(this).find('a span').text());
            }
        }
    );
    
    // Click event'i (mobil için)
    $('.has-submenu > a').click(function(e) {
        if ($(window).width() <= 768) {
            e.preventDefault();
            $(this).parent().toggleClass('submenu-open');
            $(this).parent().find('.submenu').slideToggle(200);
        }
    });
}

// Sayfa yüklendiğinde sidebar menüyü oluştur
$(document).ready(function() {
    buildSidebarMenu();
    
    // Eski submenu event'lerini kaldır (artık initializeSubmenuEvents içinde)
    // $('.has-submenu').hover(...) kısmını kaldır
});