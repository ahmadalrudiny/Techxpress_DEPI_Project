// Authentication pages JavaScript
document.addEventListener('DOMContentLoaded', function () {
  // إظهار / إخفاء كلمة المرور
  document.querySelectorAll('.toggle-password').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var wrapper = btn.closest('.input-with-toggle');
      if (!wrapper) { return; }

      var input = wrapper.querySelector('input');
      if (!input) { return; }

      var show = input.type === 'password';
      input.type = show ? 'text' : 'password';

      var icon = btn.querySelector('i');
      if (icon) { icon.className = show ? 'fas fa-eye-slash' : 'fas fa-eye'; }

      input.focus();
    });
  });

  // تحريك أول حقل عند فتح الصفحة (تجربة استخدام أفضل)
  var first = document.querySelector('.auth-card input:not([type=hidden]):not([type=radio])');
  if (first && window.innerWidth > 991) {
    setTimeout(function () { first.focus(); }, 120);
  }
});
