$(document).ready(function () {
    // "İçerik Ekle" butonuna tıklandığında modal'ı aç ve formu yükle
    $(document).on('click', '[data-bs-toggle="modal"][data-bs-target="#addReportModal"]', function (event) {
        var button = $(this);
        var url = button.data('url');
        var title = button.data('title') || 'Yeni İçerik Ekle'; // Butondan başlık al, yoksa varsayılanı kullan
        var modal = $('#addReportModal');
        
        // Modal başlığını ayarla
        modal.find('.modal-title').text(title);

        // Modal içeriğini temizle ve formu yükle
        var modalBody = modal.find('.modal-body');
        modalBody.empty().append('<p>Yükleniyor...</p>'); // Yükleniyor göstergesi

        $.get(url, function (data) {
            modalBody.html(data);
        }).fail(function () {
            modalBody.html('<p class="text-danger">İçerik yüklenirken bir hata oluştu.</p>');
        });
    });

    // Modal içindeki formu AJAX ile gönder
    $(document).on('submit', '#addReportModal form', function (event) {
        event.preventDefault(); // Formun normal submit olmasını engelle

        var form = $(this);
        var url = form.attr('action');
        var data = form.serialize();
        var updateTargetId = form.data('update-target'); // Güncellenecek elementin ID'si

        $.post(url, data, function (response) {
            if (response.success) {
                $('#addReportModal').modal('hide');

                // Hedeflenen listeyi/tabloyu güncelle
                if (updateTargetId) {
                    var updateUrl = response.updateUrl || window.location.href; // Controller'dan gelen URL'i veya mevcut sayfayı kullan
                    $.get(updateUrl, function(newContent) {
                        $(updateTargetId).html($(newContent).find(updateTargetId).html());
                    });
                } else {
                    // Eğer hedef belirtilmemişse sayfayı yeniden yükle (fallback)
                    location.reload();
                }
            } else {
                // Hata mesajlarını form içinde göster
                var modalBody = form.closest('.modal-body');
                modalBody.html(response.view); // Sunucudan dönen partial view ile formu güncelle
            }
        }).fail(function () {
            alert('Sunucuyla iletişim kurulamadı. Lütfen tekrar deneyin.');
        });
    });
});