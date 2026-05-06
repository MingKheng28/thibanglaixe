(function () {
  'use strict';

  var endpoints = {
    users: '/api/v1/admin/users',
    students: '/api/v1/admin/students',
    teachers: '/api/v1/admin/v2/teachers',
    courses: '/api/v1/admin/courses',
    classes: '/api/v1/admin/classes',
    schedules: '/api/v1/admin/v2/schedules',
    courseRegistrations: '/api/v1/admin/course-registrations',
    examRegistrations: '/api/v1/admin/exam-registrations',
    feeTypes: '/api/v1/admin/fee-types',
    topics: '/api/v1/admin/topics',
    curriculums: '/api/v1/admin/v2/curriculums',
    lessons: '/api/v1/admin/v2/lessons',
    questions: '/api/v1/admin/questions',
    exams: '/api/v1/admin/v2/exam-papers',
    examPeriods: '/api/v1/admin/exam-periods',
    examSessions: '/api/v1/admin/v2/exam-sessions',
    examResults: '/api/v1/admin/exam-results',
    certificates: '/api/v1/admin/certificates',
    receipts: '/api/v1/admin/receipts',
    notifications: '/api/v1/admin/v2/notifications'
  };

  var state = { data: {}, loading: false };
  var vi = new Intl.NumberFormat('vi-VN');
  var money = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 });

  function getToken() {
    var meta = document.querySelector('meta[name="admin-access-token"]');
    return meta ? meta.getAttribute('content') : '';
  }

  function apiUrl(path) {
    return '/Admin/ApiProxy?path=' + encodeURIComponent(path);
  }

  async function request(path, options) {
    var response = await fetch(apiUrl(path), Object.assign({ headers: { 'Content-Type': 'application/json', 'X-Admin-Access-Token': getToken() } }, options || {}));
    var text = await response.text();
    var json = text ? JSON.parse(text) : null;
    if (!response.ok) throw new Error((json && json.message) || 'Không thể gọi API quản trị.');
    return json && Object.prototype.hasOwnProperty.call(json, 'data') ? json.data : json;
  }

  function byId(id) { return document.getElementById(id); }
  function asArray(value) { return Array.isArray(value) ? value : []; }
  function fmtDate(value) { return value ? new Date(value).toLocaleString('vi-VN') : '—'; }
  function fmtDateOnly(value) { return value ? new Date(value).toLocaleDateString('vi-VN') : '—'; }
  function esc(value) { return String(value == null ? '' : value).replace(/[&<>'"]/g, function (c) { return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[c]; }); }

  function statusLabel(status) {
    var map = {
      hoat_dong: 'Hoạt động', dang_mo: 'Đang mở', tam_khoa: 'Tạm khóa', cho_duyet: 'Chờ duyệt', da_duyet: 'Đã duyệt',
      cho_xac_nhan: 'Chờ xác nhận', da_xac_nhan: 'Đã xác nhận', da_huy: 'Đã hủy', dang_hoc: 'Đang học',
      nhap: 'Bản nháp', dat: 'Đạt', khong_dat: 'Không đạt', active: 'Hoạt động', issued: 'Đã cấp',
      ly_thuyet: 'Lý thuyết', thuc_hanh: 'Thực hành', on_tap: 'Ôn tập', thi_thu: 'Thi thử', thi_sat_hach: 'Thi sát hạch',
      sap_dien_ra: 'Sắp diễn ra', ngung_day: 'Ngừng dạy', sat_hach: 'Sát hạch', info: 'Thông tin', warning: 'Cảnh báo', error: 'Lỗi'
    };
    return map[status] || (status ? String(status).replace(/_/g, ' ') : 'Không rõ');
  }

  function badge(status) {
    var cls = /hoat|dang|duyet|xac_nhan|dat|issued|active|ly_thuyet|thuc_hanh|on_tap|sat_hach|info/.test(status || '') ? 'badge-green'
      : /cho|nhap|sap|warning/.test(status || '') ? 'badge-amber'
      : /huy|khoa|khong|error|ngung/.test(status || '') ? 'badge-red' : 'badge-gray';
    return '<span class="badge ' + cls + '">' + esc(statusLabel(status)) + '</span>';
  }

  function toast(message, type) {
    var root = document.querySelector('.admin-toast') || document.body.appendChild(Object.assign(document.createElement('div'), { className: 'admin-toast' }));
    var item = document.createElement('div');
    item.className = 'toast-item ' + (type || '');
    item.textContent = message;
    root.appendChild(item);
    setTimeout(function () { item.remove(); }, 3200);
  }

  function setText(id, value) { var el = byId(id); if (el) el.textContent = value; }
  function rows(id, html, colspan) {
    var el = byId(id);
    if (!el) return;
    el.innerHTML = html || '<tr><td colspan="' + (colspan || 6) + '" class="empty">Chưa có dữ liệu từ API.</td></tr>';
  }

  async function loadAll() {
    state.loading = true;
    try {
      var keys = Object.keys(endpoints);
      var results = await Promise.all(keys.map(function (key) {
        return request(endpoints[key]).then(function (data) { return [key, asArray(data)]; }).catch(function () { return [key, []]; });
      }));
      results.forEach(function (pair) { state.data[pair[0]] = pair[1]; });
      renderAll();
      toast('Đã đồng bộ dữ liệu quản trị từ backend.', 'success');
    } catch (err) {
      toast(err.message, 'error');
    } finally { state.loading = false; }
  }

  function renderAll() {
    var d = state.data;
    var activeEnrollments = asArray(d.students).reduce(function (sum, student) { return sum + asArray(student.classes).filter(function (x) { return x.trang_thai === 'dang_hoc'; }).length; }, 0);
    var approvedProfiles = asArray(d.students).filter(function (student) { return asArray(student.profiles).some(function (profile) { return profile.trang_thai === 'da_duyet'; }); }).length;
    var approvedCourseRegistrations = asArray(d.courseRegistrations).filter(function (x) { return x.trang_thai === 'da_duyet'; }).length;
    setText('stat-users', vi.format(asArray(d.users).length));
    setText('stat-students', vi.format(asArray(d.students).length));
    setText('stat-courses', vi.format(asArray(d.courses).length));
    setText('stat-exams', vi.format(asArray(d.exams).length));
    setText('stat-questions', vi.format(asArray(d.questions).length));
    setText('stat-receipts', vi.format(asArray(d.receipts).filter(function (x) { return x.trang_thai === 'cho_xac_nhan'; }).length));
    setText('stat-results', vi.format(asArray(d.examResults).length));
    setText('stat-certificates', vi.format(asArray(d.certificates).length));
    setText('stat-revenue', money.format(asArray(d.receipts).reduce(function (s, x) { return s + Number(x.tong_tien || x.tongTien || 0); }, 0)));
    setText('stat-active-enrollments', vi.format(activeEnrollments));
    setText('stat-approved-profiles', vi.format(approvedProfiles));
    setText('stat-approved-course-registrations', vi.format(approvedCourseRegistrations));
    setText('stat-fee-types', vi.format(asArray(d.feeTypes).length));

    rows('users-body', asArray(d.users).map(function (x) {
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.ten_dang_nhap) + '</td><td>' + esc(x.email) + '</td><td>' + esc(asArray(x.roles).map(function (r) { return r.ten_vai_tro; }).join(', ') || '—') + '</td><td>' + badge(x.trang_thai) + '</td><td class="mono">' + fmtDate(x.created_at) + '</td><td><button class="btn" data-action="toggle-user" data-id="' + x.id + '" data-status="' + (x.trang_thai === 'hoat_dong' ? 'tam_khoa' : 'hoat_dong') + '">Đổi trạng thái</button></td></tr>';
    }).join(''), 7);

    rows('students-body', asArray(d.students).map(function (x) {
      var user = x.user || {};
      var activeClass = asArray(x.classes).find(function (item) { return item.trang_thai === 'dang_hoc'; }) || asArray(x.classes)[0] || {};
      var profile = asArray(x.profiles).find(function (item) { return item.trang_thai === 'da_duyet'; }) || asArray(x.profiles)[0] || {};
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.ho_ten) + '</td><td>' + esc(user.email || '—') + '</td><td>' + esc(user.so_dien_thoai || '—') + '</td><td>' + esc(activeClass.class_name || 'Chưa vào lớp') + '</td><td>' + badge(profile.trang_thai || 'cho_nop') + '</td><td>' + badge(user.trang_thai) + '</td><td class="mono">' + fmtDateOnly(activeClass.ngay_vao_lop) + '</td></tr>';
    }).join(''), 8);

    rows('teachers-body', asArray(d.teachers).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_giao_vien || ('#' + x.id)) + '</td><td class="bold">' + esc(x.ho_ten || x.ten_dang_nhap || '—') + '</td><td>' + esc(x.email || '—') + '</td><td>' + esc(x.so_dien_thoai || '—') + '</td><td>' + esc(x.hang_gplx || '—') + '</td><td>' + vi.format(x.class_count || 0) + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 7);

    rows('courses-body', asArray(d.courses).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_khoa_hoc) + '</td><td class="bold">' + esc(x.ten_khoa_hoc) + '</td><td>' + money.format(Number(x.hoc_phi || 0)) + '</td><td>' + esc(x.thoi_luong || '—') + '</td><td>' + badge(x.trang_thai) + '</td><td><button class="btn btn-danger" data-action="delete-course" data-id="' + x.id + '">Xóa</button></td></tr>';
    }).join(''), 6);

    rows('classes-body', asArray(d.classes).map(function (x) {
      var next = x.next_schedule || {};
      return '<tr><td class="mono">' + esc(x.ma_lop) + '</td><td class="bold">' + esc(x.ten_lop) + '</td><td>' + esc(x.course_name || ('#' + x.khoa_hoc_id)) + '</td><td>' + fmtDateOnly(x.ngay_bat_dau) + '</td><td>' + fmtDateOnly(x.ngay_ket_thuc) + '</td><td class="bold">' + vi.format(x.current_students || 0) + '/' + vi.format(x.si_so_toi_da || 0) + '</td><td>' + vi.format(x.schedule_count || 0) + '</td><td>' + (next.ngay_hoc ? fmtDateOnly(next.ngay_hoc) + ' · ' + esc(next.gio_bat_dau || '') : '—') + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 9);

    rows('schedules-body', asArray(d.schedules).map(function (x) {
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.ten_buoi) + '</td><td>' + esc(x.ma_lop || ('#' + x.lop_hoc_id)) + '</td><td>' + badge(x.loai_buoi) + '</td><td>' + fmtDateOnly(x.ngay_hoc) + '</td><td class="mono">' + esc(x.gio_bat_dau || '') + ' - ' + esc(x.gio_ket_thuc || '') + '</td><td>' + esc(x.teacher_name || '—') + '</td><td>' + esc(x.dia_diem || x.phong_hoc || '—') + '</td></tr>';
    }).join(''), 8);

    rows('curriculums-body', asArray(d.curriculums).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_giao_trinh) + '</td><td class="bold">' + esc(x.ten_giao_trinh) + '</td><td>' + esc(x.hang_bang || '—') + '</td><td>' + vi.format(x.lesson_count || 0) + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 5);

    rows('lessons-body', asArray(d.lessons).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_bai_hoc) + '</td><td class="bold">' + esc(x.ten_bai_hoc) + '</td><td>' + esc(x.ten_giao_trinh || ('#' + x.giao_trinh_id)) + '</td><td>' + badge(x.loai_bai_hoc) + '</td><td>' + vi.format(x.thu_tu || 0) + '</td><td>' + (x.thoi_luong_phut ? vi.format(x.thoi_luong_phut) + ' phút' : '—') + '</td></tr>';
    }).join(''), 6);

    rows('topics-body', asArray(d.topics).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_chu_de) + '</td><td class="bold">' + esc(x.ten_chu_de) + '</td><td>' + esc(x.mo_ta || '—') + '</td><td>' + vi.format(x.question_count || 0) + '</td></tr>';
    }).join(''), 4);

    rows('questions-body', asArray(d.questions).slice(0, 80).map(function (x) {
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.noi_dung || '').slice(0, 120) + '</td><td>#' + esc(x.chu_de_id) + '</td><td>' + esc(x.muc_do || '—') + '</td><td>' + (x.la_cau_diem_liet ? '<span class="badge badge-red">Điểm liệt</span>' : '<span class="badge badge-blue">Thường</span>') + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 6);

    rows('exams-body', asArray(d.exams).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_de_thi) + '</td><td class="bold">' + esc(x.ten_de_thi) + '</td><td>' + vi.format(x.question_count || x.tong_so_cau || 0) + '</td><td>' + vi.format(x.thoi_gian_lam_bai || 0) + ' phút</td><td>' + badge(x.loai_de_thi || '—') + '</td><td>' + (x.is_public ? '<span class="badge badge-green">Đã công bố</span>' : '<span class="badge badge-amber">Chưa công bố</span>') + '</td><td>' + badge(x.trang_thai) + '</td><td><button class="btn" data-action="publish-exam" data-id="' + x.id + '">Công bố</button></td></tr>';
    }).join(''), 8);

    rows('exam-periods-body', asArray(d.examPeriods).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_ky_thi) + '</td><td class="bold">' + esc(x.ten_ky_thi) + '</td><td>' + fmtDateOnly(x.ngay_thi) + '</td><td>' + esc(x.dia_diem || '—') + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 5);

    rows('exam-sessions-body', asArray(d.examSessions).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_ca_thi) + '</td><td class="bold">' + esc(x.ten_ca_thi) + '</td><td>' + esc(x.ten_ky_thi || x.ma_ky_thi || '—') + '</td><td>' + fmtDateOnly(x.ngay_thi) + '</td><td class="mono">' + esc(x.gio_bat_dau || '') + ' - ' + esc(x.gio_ket_thuc || '') + '</td><td>' + esc(x.examiner_name || '—') + '</td><td>' + vi.format(x.registration_count || 0) + '/' + vi.format(x.so_luong_toi_da || 0) + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 8);

    rows('exam-results-body', asArray(d.examResults).map(function (x) {
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.student_name) + '</td><td>' + vi.format(x.so_cau_dung || 0) + '/' + vi.format(x.tong_so_cau || 0) + '</td><td class="bold">' + esc(x.diem) + '</td><td>' + badge(x.ket_qua) + '</td><td class="mono">' + fmtDate(x.created_at) + '</td></tr>';
    }).join(''), 6);

    rows('certificates-body', asArray(d.certificates).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_chung_chi) + '</td><td class="bold">' + esc(x.student_name) + '</td><td>' + fmtDateOnly(x.ngay_cap) + '</td><td>' + fmtDateOnly(x.ngay_het_han) + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 5);

    rows('course-registrations-body', asArray(d.courseRegistrations).map(function (x) {
      var assigned = asArray(x.assigned_classes).map(function (item) { return item.class_name; }).join(', ') || 'Chưa gán lớp';
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.student_name) + '</td><td>' + esc(x.course_name) + '</td><td>' + esc(assigned) + '</td><td>' + money.format(Number(x.course_fee || 0)) + '</td><td>' + badge(x.trang_thai) + '</td><td class="mono">' + fmtDate(x.ngay_duyet || x.ngay_dang_ky) + '</td><td><button class="btn" data-action="approve-course-registration" data-id="' + x.id + '">Duyệt</button></td></tr>';
    }).join(''), 8);

    rows('exam-registrations-body', asArray(d.examRegistrations).map(function (x) {
      return '<tr><td class="mono">#' + x.id + '</td><td class="bold">' + esc(x.student_name) + '</td><td>' + esc(x.exam_period_name || '—') + '</td><td>' + esc(x.exam_session_name || x.exam_session_code || '—') + '</td><td>' + fmtDateOnly(x.exam_date) + '</td><td>' + badge(x.trang_thai) + '</td><td><button class="btn" data-action="approve-exam-registration" data-id="' + x.id + '">Duyệt</button></td></tr>';
    }).join(''), 7);

    rows('fee-types-body', asArray(d.feeTypes).map(function (x) {
      return '<tr><td class="mono">' + esc(x.ma_loai) + '</td><td class="bold">' + esc(x.ten_loai) + '</td><td>' + money.format(Number(x.so_tien_mac_dinh || 0)) + '</td><td>' + esc(x.mo_ta || '—') + '</td><td>' + badge(x.trang_thai) + '</td></tr>';
    }).join(''), 5);

    rows('receipts-body', asArray(d.receipts).map(function (x) {
      var feeTypes = asArray(x.details).map(function (detail) { return detail.fee_type_name || detail.fee_type_code; }).join(', ') || '—';
      return '<tr><td class="mono">' + esc(x.ma_phieu_thu) + '</td><td class="bold">' + esc(x.student_name || '—') + '</td><td>' + esc(feeTypes) + '</td><td class="bold">' + money.format(Number(x.tong_tien || 0)) + '</td><td>' + badge(x.trang_thai) + '</td><td class="mono">' + fmtDate(x.ngay_thu) + '</td><td><button class="btn" data-action="confirm-receipt" data-id="' + x.id + '">Xác nhận</button> <button class="btn btn-danger" data-action="cancel-receipt" data-id="' + x.id + '">Hủy</button></td></tr>';
    }).join(''), 7);

    rows('notifications-body', asArray(d.notifications).map(function (x) {
      return '<tr><td class="mono">#' + x.id + '</td><td>' + esc(x.loai || '—') + '</td><td>' + badge(x.muc_do || 'info') + '</td><td class="bold">' + esc(x.tieu_de || '—') + '</td><td>' + esc(x.doi_tuong || '—') + '</td><td>' + (x.da_doc ? '<span class="badge badge-gray">Đã đọc</span>' : '<span class="badge badge-amber">Chưa đọc</span>') + '</td><td class="mono">' + fmtDate(x.created_at) + '</td><td>' + (x.da_doc ? '—' : '<button class="btn" data-action="read-notification" data-id="' + x.id + '">Đánh dấu đã đọc</button>') + '</td></tr>';
    }).join(''), 8);
  }

  function updateBreadcrumb(nav) {
    var groupEl = byId('admin-breadcrumb-group');
    var titleEl = byId('admin-breadcrumb-title');
    if (!groupEl || !titleEl) return;
    var group = nav && nav.dataset.group ? nav.dataset.group : 'Tổng quan';
    var title = nav && nav.dataset.title ? nav.dataset.title : 'Dashboard';
    groupEl.textContent = group;
    titleEl.textContent = title;
    document.title = title + ' · ' + group + ' · Admin GPLX Portal';
  }

  function activate(section) {
    var nav = document.querySelector('.nav-item[data-section="' + section + '"]') || document.querySelector('.nav-item[data-section="dashboard"]');
    var targetSection = nav ? nav.dataset.section : 'dashboard';
    document.querySelectorAll('.admin-section').forEach(function (el) { el.classList.toggle('active', el.id === targetSection); });
    document.querySelectorAll('.nav-item[data-section]').forEach(function (el) { el.classList.toggle('active', el === nav); });
    updateBreadcrumb(nav);
    history.replaceState(null, '', '#' + targetSection);
  }

  async function mutate(path, method, body) {
    await request(path, { method: method, body: body ? JSON.stringify(body) : undefined });
    await loadAll();
  }

  document.addEventListener('click', function (e) {
    var nav = e.target.closest('[data-section]');
    if (nav) { e.preventDefault(); activate(nav.dataset.section); return; }
    var action = e.target.closest('[data-action]');
    if (!action) return;
    var id = action.dataset.id;
    if (action.dataset.action === 'toggle-user') mutate('/api/v1/admin/users/' + id + '/status', 'PATCH', { status: action.dataset.status });
    if (action.dataset.action === 'delete-course' && confirm('Xóa khóa học này?')) mutate('/api/v1/admin/courses/' + id, 'DELETE');
    if (action.dataset.action === 'approve-course-registration') mutate('/api/v1/admin/course-registrations/' + id + '/approve', 'PATCH');
    if (action.dataset.action === 'approve-exam-registration') mutate('/api/v1/admin/exam-registrations/' + id + '/approve', 'PATCH');
    if (action.dataset.action === 'confirm-receipt') mutate('/api/v1/admin/receipts/' + id + '/confirm', 'PATCH');
    if (action.dataset.action === 'cancel-receipt') mutate('/api/v1/admin/receipts/' + id + '/cancel', 'PATCH');
    if (action.dataset.action === 'publish-exam') mutate('/api/v1/admin/v2/exam-papers/' + id + '/publish', 'PATCH');
    if (action.dataset.action === 'read-notification') mutate('/api/v1/admin/v2/notifications/' + id + '/read', 'PATCH');
  });

  document.addEventListener('submit', function (e) {
    var form = e.target.closest('[data-create]');
    if (!form) return;
    e.preventDefault();
    var data = Object.fromEntries(new FormData(form).entries());
    if (form.dataset.create === 'course') {
      mutate('/api/v1/admin/courses', 'POST', { code: data.code, name: data.name, description: data.description, fee: Number(data.fee || 0), duration: Number(data.duration || 0), status: data.status || 'dang_mo' }).then(function () { form.reset(); });
    }
    if (form.dataset.create === 'class') {
      mutate('/api/v1/admin/classes', 'POST', { courseId: Number(data.courseId), code: data.code, name: data.name, teacherId: data.teacherId ? Number(data.teacherId) : null, startDate: data.startDate || null, endDate: data.endDate || null, maxStudents: Number(data.maxStudents || 30), status: data.status || 'dang_mo' }).then(function () { form.reset(); });
    }
    if (form.dataset.create === 'schedule') {
      mutate('/api/v1/admin/v2/schedules', 'POST', { classId: Number(data.classId), name: data.name, type: data.type || 'ly_thuyet', teacherId: data.teacherId ? Number(data.teacherId) : null, studyDate: data.studyDate, startTime: data.startTime, endTime: data.endTime, content: data.content, room: data.room, location: data.location }).then(function () { form.reset(); });
    }
    if (form.dataset.create === 'exam') {
      mutate('/api/v1/admin/exams', 'POST', { code: data.code, name: data.name, examPeriodId: Number(data.examPeriodId), totalQuestions: Number(data.totalQuestions), durationMinutes: Number(data.durationMinutes), status: data.status || 'nhap', type: data.type || 'sat_hach' }).then(function () { form.reset(); });
    }
    if (form.dataset.create === 'curriculum') {
      mutate('/api/v1/admin/v2/curriculums', 'POST', { code: data.code, name: data.name, licenseClass: data.licenseClass, description: data.description, status: data.status || 'active' }).then(function () { form.reset(); });
    }
  });

  window.AdminUnified = { loadAll: loadAll, activate: activate };
  document.addEventListener('DOMContentLoaded', function () {
    activate((location.hash || '#dashboard').slice(1));
    loadAll();
  });
})();
