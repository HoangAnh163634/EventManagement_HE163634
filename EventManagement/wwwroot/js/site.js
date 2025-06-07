
$(document).ready(function () {
    // Fade-in effect for hero text
    $('.hero-text').css('opacity', 0).animate({ opacity: 1 }, 1500);

    // Format dates with Moment.js
    $('.event-date').each(function () {
        var date = moment($(this).text());
        $(this).text(date.format('MMMM DD, YYYY, h:mm A'));
    });

    // Hover effect for event cards
    $('.event-card').hover(
        function () { $(this).addClass('shadow-lg').css('transform', 'scale(1.05)'); },
        function () { $(this).removeClass('shadow-lg').css('transform', 'scale(1)'); }
    );

    // Parallax effect for hero
    $(window).scroll(function () {
        var scroll = $(window).scrollTop();
        $('.parallax-overlay').css('background-position', 'center ' + (scroll * 0.5) + 'px');
        if ($(this).scrollTop() > 50) {
            $('.navbar').addClass('shadow-lg');
        } else {
            $('.navbar').removeClass('shadow-lg');
        }
    });

    // SweetAlert welcome with custom styling
    Swal.fire({
        title: 'Welcome!',
        text: 'Elevate your event experience with us.',
        icon: 'info',
        confirmButtonText: 'Explore Now',
        background: '#0F0F0F',
        color: '#E0C68F',
        customClass: {
            popup: 'animated fadeIn'
        }
    });

    // Tool hover effect
    $('.tool-card').hover(
        function () {
            var content = $(this).data('content');
            $(this).append('<div class="tool-tooltip">' + content + '</div>');
            $('.tool-tooltip').fadeIn(300);
        },
        function () {
            $('.tool-tooltip').remove();
        }
    ).mousemove(function(e) {
        $('.tool-tooltip').css({
            left: e.pageX + 10,
            top: e.pageY - 20
        });
    });

    // Ensure carousel works correctly
    $('.carousel').carousel({
        interval: 5000,
        pause: 'hover'
    });
});
