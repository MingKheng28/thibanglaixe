--------------------------------------------------------------------------------
Table: dbo.bai_thi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  hoc_vien_id | bigint | NOT NULL |  | 
  de_thi_id | bigint | NOT NULL |  | 
  ca_thi_id | bigint | NOT NULL |  | 
  thoi_gian_bat_dau | datetime2 | NULL |  | 
  thoi_gian_nop | datetime2 | NULL |  | 
  tong_so_cau | int | NOT NULL | ((0)) | 
  so_cau_dung | int | NOT NULL | ((0)) | 
  diem | decimal(5,2) | NOT NULL | ((0)) | 
  ket_qua | varchar(20) | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('chua_lam') | 
 
Indexes / Primary Keys:
  PK__bai_thi__3213E83FFA944E62 PRIMARY KEY (id)
 
Other Indexes:
  ix_bai_thi_hoc_vien_id (hoc_vien_id) NONCLUSTERED
  ix_bai_thi_de_thi_id (de_thi_id) NONCLUSTERED
 
Foreign Keys:
  fk_bai_thi_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_bai_thi_de_thi FOREIGN KEY (de_thi_id) REFERENCES [dbo].[de_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_bai_thi_ca_thi FOREIGN KEY (ca_thi_id) REFERENCES [dbo].[ca_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_bai_thi_tong_so_cau CHECK (([tong_so_cau]>=(0)))
  ck_bai_thi_so_cau_dung CHECK (([so_cau_dung]>=(0)))
  ck_bai_thi_diem CHECK (([diem]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.buoi_hoc
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  lop_hoc_id | bigint | NOT NULL |  | 
  ten_buoi | nvarchar(150) | NOT NULL |  | 
  ngay_hoc | date | NOT NULL |  | 
  gio_bat_dau | time | NOT NULL |  | 
  gio_ket_thuc | time | NOT NULL |  | 
  noi_dung | nvarchar(500) | NULL |  | 
  phong_hoc | nvarchar(100) | NULL |  | 
  loai_buoi | varchar(30) | NOT NULL | ('ly_thuyet') | 
  dia_diem | nvarchar(255) | NULL |  | 
  giao_vien_id | bigint | NULL |  | 
  ghi_chu | nvarchar(500) | NULL |  | 
 
Indexes / Primary Keys:
  PK__buoi_hoc__3213E83F7D4F244E PRIMARY KEY (id)
 
Other Indexes:
  ix_buoi_hoc_lop_hoc_id (lop_hoc_id) NONCLUSTERED
  ix_buoi_hoc_loai_ngay (loai_buoi, ngay_hoc) NONCLUSTERED
 
Foreign Keys:
  fk_buoi_hoc_lop_hoc FOREIGN KEY (lop_hoc_id) REFERENCES [dbo].[lop_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_buoi_hoc_giao_vien_ho_so FOREIGN KEY (giao_vien_id) REFERENCES [dbo].[giao_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_buoi_hoc_gio CHECK (([gio_ket_thuc]>[gio_bat_dau]))
 


--------------------------------------------------------------------------------
Table: dbo.ca_thi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ky_thi_id | bigint | NOT NULL |  | 
  ma_ca_thi | varchar(30) | NOT NULL |  | 
  ten_ca_thi | nvarchar(150) | NOT NULL |  | 
  gio_bat_dau | time | NOT NULL |  | 
  gio_ket_thuc | time | NOT NULL |  | 
  phong_thi | nvarchar(100) | NULL |  | 
  so_luong_toi_da | int | NOT NULL | ((0)) | 
  giam_thi_id | bigint | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('sap_dien_ra') | 
 
Indexes / Primary Keys:
  PK__ca_thi__3213E83FEFCBDEFE PRIMARY KEY (id)
 
Other Indexes:
  uq_ca_thi_ma (ma_ca_thi) NONCLUSTERED UNIQUE
  ix_ca_thi_ky_thi_id (ky_thi_id) NONCLUSTERED
 
Foreign Keys:
  fk_ca_thi_ky_thi FOREIGN KEY (ky_thi_id) REFERENCES [dbo].[ky_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ca_thi_giam_thi FOREIGN KEY (giam_thi_id) REFERENCES [dbo].[giao_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_ca_thi_gio CHECK (([gio_ket_thuc]>[gio_bat_dau]))
  ck_ca_thi_so_luong CHECK (([so_luong_toi_da]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.categories
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  parent_id | bigint | NULL |  | 
  ma_danh_muc | varchar(50) | NOT NULL |  | 
  ten_danh_muc | nvarchar(150) | NOT NULL |  | 
  slug | varchar(200) | NOT NULL |  | 
  mo_ta | nvarchar(500) | NULL |  | 
  is_active | bit | NOT NULL | ((1)) | 
  created_by | bigint | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__categori__3213E83FFAA569D6 PRIMARY KEY (id)
 
Other Indexes:
  uq_categories_slug (slug) NONCLUSTERED UNIQUE
  uq_categories_ma_danh_muc (ma_danh_muc) NONCLUSTERED UNIQUE
  ix_categories_parent_id (parent_id) NONCLUSTERED
  ix_categories_is_active (is_active) NONCLUSTERED
 
Foreign Keys:
  fk_categories_parent FOREIGN KEY (parent_id) REFERENCES [dbo].[categories](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_categories_created_by FOREIGN KEY (created_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.cau_hoi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  chu_de_id | bigint | NOT NULL |  | 
  noi_dung | nvarchar | NOT NULL |  | 
  loai_cau_hoi | varchar(30) | NOT NULL | ('trac_nghiem') | 
  muc_do | varchar(30) | NULL |  | 
  la_cau_diem_liet | bit | NOT NULL | ((0)) | 
  trang_thai | varchar(30) | NOT NULL | ('hoat_dong') | 
  giai_thich_dap_an | nvarchar(2000) | NULL |  | 
  ma_cau_hoi | varchar(30) | NULL |  | 
  hang_bang | nvarchar(20) | NULL |  | 
  created_by | bigint | NULL |  | 
  updated_by | bigint | NULL |  | 
  approved_by | bigint | NULL |  | 
  approved_at | datetime2 | NULL |  | 
  created_at | datetime2 | NOT NULL | (sysutcdatetime()) | 
  updated_at | datetime2 | NOT NULL | (sysutcdatetime()) | 
 
Indexes / Primary Keys:
  PK__cau_hoi__3213E83FF2E2AB91 PRIMARY KEY (id)
 
Other Indexes:
  ix_cau_hoi_chu_de_id (chu_de_id) NONCLUSTERED
  uq_cau_hoi_ma (ma_cau_hoi) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_cau_hoi_chu_de FOREIGN KEY (chu_de_id) REFERENCES [dbo].[chu_de_cau_hoi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_cau_hoi_created_by FOREIGN KEY (created_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_cau_hoi_updated_by FOREIGN KEY (updated_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_cau_hoi_approved_by FOREIGN KEY (approved_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.certificates
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_chung_chi | varchar(50) | NOT NULL |  | 
  hoc_vien_id | bigint | NOT NULL |  | 
  exam_result_id | bigint | NOT NULL |  | 
  ngay_cap | datetime2 | NOT NULL |  | 
  ngay_het_han | datetime2 | NULL |  | 
  trang_thai | varchar(30) | NOT NULL |  | 
  certificate_file_id | bigint | NULL |  | 
  created_by | bigint | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__certific__3213E83F590B0499 PRIMARY KEY (id)
 
Other Indexes:
  uq_certificates_exam_result_id (exam_result_id) NONCLUSTERED UNIQUE
  uq_certificates_ma_chung_chi (ma_chung_chi) NONCLUSTERED UNIQUE
  ix_certificates_hoc_vien_id (hoc_vien_id) NONCLUSTERED
  ix_certificates_trang_thai (trang_thai) NONCLUSTERED
  ix_certificates_ngay_cap (ngay_cap) NONCLUSTERED
 
Foreign Keys:
  fk_certificates_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_certificates_exam_result FOREIGN KEY (exam_result_id) REFERENCES [dbo].[exam_results](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_certificates_file FOREIGN KEY (certificate_file_id) REFERENCES [dbo].[files](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_certificates_created_by FOREIGN KEY (created_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_certificates_trang_thai CHECK (([trang_thai]='expired' OR [trang_thai]='revoked' OR [trang_thai]='valid'))
  ck_certificates_ngay CHECK (([ngay_het_han] IS NULL OR [ngay_het_han]>=[ngay_cap]))
 


--------------------------------------------------------------------------------
Table: dbo.chi_tiet_bai_thi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  bai_thi_id | bigint | NOT NULL |  | 
  cau_hoi_id | bigint | NOT NULL |  | 
  dap_an_chon_id | bigint | NULL |  | 
  la_dung | bit | NULL |  | 
 
Indexes / Primary Keys:
  PK__chi_tiet__3213E83FA5CFAC42 PRIMARY KEY (id)
 
Other Indexes:
  uq_ctbt (bai_thi_id, cau_hoi_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_ctbt_bai_thi FOREIGN KEY (bai_thi_id) REFERENCES [dbo].[bai_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ctbt_cau_hoi FOREIGN KEY (cau_hoi_id) REFERENCES [dbo].[cau_hoi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ctbt_dap_an FOREIGN KEY (dap_an_chon_id) REFERENCES [dbo].[dap_an](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.chi_tiet_phieu_thu
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  phieu_thu_id | bigint | NOT NULL |  | 
  loai_khoan_thu_id | bigint | NOT NULL |  | 
  so_tien | decimal(18,2) | NOT NULL |  | 
  ghi_chu | nvarchar(255) | NULL |  | 
 
Indexes / Primary Keys:
  PK__chi_tiet__3213E83F5673FF69 PRIMARY KEY (id)
 
Foreign Keys:
  fk_ctpt_phieu_thu FOREIGN KEY (phieu_thu_id) REFERENCES [dbo].[phieu_thu](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ctpt_loai_khoan_thu FOREIGN KEY (loai_khoan_thu_id) REFERENCES [dbo].[loai_khoan_thu](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_ctpt_so_tien CHECK (([so_tien]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.chu_de_cau_hoi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_chu_de | varchar(30) | NOT NULL |  | 
  ten_chu_de | nvarchar(150) | NOT NULL |  | 
  mo_ta | nvarchar(255) | NULL |  | 
  bai_hoc_id | bigint | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('active') | 
 
Indexes / Primary Keys:
  PK__chu_de_c__3213E83F55E017A7 PRIMARY KEY (id)
 
Other Indexes:
  uq_chu_de_cau_hoi_ma (ma_chu_de) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_chu_de_cau_hoi_bai_hoc FOREIGN KEY (bai_hoc_id) REFERENCES [dbo].[bai_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.dang_ky_du_thi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  hoc_vien_id | bigint | NOT NULL |  | 
  ca_thi_id | bigint | NOT NULL |  | 
  ngay_dang_ky | datetime2 | NOT NULL | (getdate()) | 
  trang_thai | varchar(30) | NOT NULL | ('cho_duyet') | 
  nguoi_duyet_id | bigint | NULL |  | 
  ngay_duyet | datetime2 | NULL |  | 
 
Indexes / Primary Keys:
  PK__dang_ky___3213E83F75AB48B5 PRIMARY KEY (id)
 
Other Indexes:
  uq_dang_ky_du_thi (hoc_vien_id, ca_thi_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_dkdt_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_dkdt_ca_thi FOREIGN KEY (ca_thi_id) REFERENCES [dbo].[ca_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_dkdt_nguoi_duyet FOREIGN KEY (nguoi_duyet_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.dang_ky_khoa_hoc
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  hoc_vien_id | bigint | NOT NULL |  | 
  khoa_hoc_id | bigint | NOT NULL |  | 
  ngay_dang_ky | datetime2 | NOT NULL | (getdate()) | 
  trang_thai | varchar(30) | NOT NULL | ('cho_duyet') | 
  nguoi_duyet_id | bigint | NULL |  | 
  ngay_duyet | datetime2 | NULL |  | 
 
Indexes / Primary Keys:
  PK__dang_ky___3213E83F987822C9 PRIMARY KEY (id)
 
Other Indexes:
  uq_dang_ky_khoa_hoc (hoc_vien_id, khoa_hoc_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_dk_khoa_hoc_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_dk_khoa_hoc_khoa_hoc FOREIGN KEY (khoa_hoc_id) REFERENCES [dbo].[khoa_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_dk_khoa_hoc_nguoi_duyet FOREIGN KEY (nguoi_duyet_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.dap_an
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  cau_hoi_id | bigint | NOT NULL |  | 
  noi_dung | nvarchar(1000) | NOT NULL |  | 
  la_dap_an_dung | bit | NOT NULL | ((0)) | 
  thu_tu | int | NOT NULL |  | 
 
Indexes / Primary Keys:
  PK__dap_an__3213E83F83C240A7 PRIMARY KEY (id)
 
Other Indexes:
  uq_dap_an_thu_tu (cau_hoi_id, thu_tu) NONCLUSTERED UNIQUE
  ix_dap_an_cau_hoi_id (cau_hoi_id) NONCLUSTERED
 
Foreign Keys:
  fk_dap_an_cau_hoi FOREIGN KEY (cau_hoi_id) REFERENCES [dbo].[cau_hoi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.de_thi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_de_thi | varchar(30) | NOT NULL |  | 
  ten_de_thi | nvarchar(150) | NOT NULL |  | 
  ky_thi_id | bigint | NOT NULL |  | 
  tong_so_cau | int | NOT NULL | ((0)) | 
  thoi_gian_lam_bai | int | NOT NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('nhap') | 
  nguoi_tao_id | bigint | NULL |  | 
  ngay_tao | datetime2 | NOT NULL | (getdate()) | 
  loai_de_thi | nvarchar(50) | NULL |  | 
  published_at | datetime2 | NULL |  | 
  published_by | bigint | NULL |  | 
  hang_bang | nvarchar(20) | NULL |  | 
  is_public | bit | NOT NULL | ((0)) | 
 
Indexes / Primary Keys:
  PK__de_thi__3213E83F7F94DEC5 PRIMARY KEY (id)
 
Other Indexes:
  uq_de_thi_ma (ma_de_thi) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_de_thi_published_by FOREIGN KEY (published_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_de_thi_ky_thi FOREIGN KEY (ky_thi_id) REFERENCES [dbo].[ky_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_de_thi_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_de_thi_tong_so_cau CHECK (([tong_so_cau]>=(0)))
  ck_de_thi_thoi_gian CHECK (([thoi_gian_lam_bai]>(0)))
 


--------------------------------------------------------------------------------
Table: dbo.de_thi_cau_hoi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  de_thi_id | bigint | NOT NULL |  | 
  cau_hoi_id | bigint | NOT NULL |  | 
  thu_tu_cau | int | NOT NULL |  | 
 
Indexes / Primary Keys:
  PK__de_thi_c__3213E83FF142EF3A PRIMARY KEY (id)
 
Other Indexes:
  uq_de_thi_thu_tu (de_thi_id, thu_tu_cau) NONCLUSTERED UNIQUE
  uq_de_thi_cau_hoi (de_thi_id, cau_hoi_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_dtch_de_thi FOREIGN KEY (de_thi_id) REFERENCES [dbo].[de_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_dtch_cau_hoi FOREIGN KEY (cau_hoi_id) REFERENCES [dbo].[cau_hoi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.diem_danh
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  buoi_hoc_id | bigint | NOT NULL |  | 
  hoc_vien_id | bigint | NOT NULL |  | 
  trang_thai | varchar(30) | NOT NULL |  | 
  ghi_chu | nvarchar(255) | NULL |  | 
  giao_vien_id | bigint | NULL |  | 
  thoi_gian_diem_danh | datetime2 | NOT NULL | (getdate()) | 
  giao_vien_ho_so_id | bigint | NULL |  | 
 
Indexes / Primary Keys:
  PK__diem_dan__3213E83F381302A2 PRIMARY KEY (id)
 
Other Indexes:
  uq_diem_danh (buoi_hoc_id, hoc_vien_id) NONCLUSTERED UNIQUE
  ix_diem_danh_buoi_hoc_id (buoi_hoc_id) NONCLUSTERED
 
Foreign Keys:
  fk_diem_danh_buoi_hoc FOREIGN KEY (buoi_hoc_id) REFERENCES [dbo].[buoi_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_diem_danh_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_diem_danh_giao_vien FOREIGN KEY (giao_vien_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_diem_danh_giao_vien_ho_so FOREIGN KEY (giao_vien_ho_so_id) REFERENCES [dbo].[giao_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.exam_results
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  bai_thi_id | bigint | NOT NULL |  | 
  hoc_vien_id | bigint | NOT NULL |  | 
  tong_so_cau | int | NOT NULL |  | 
  so_cau_dung | int | NOT NULL |  | 
  diem | decimal(5,2) | NOT NULL |  | 
  ket_qua | varchar(20) | NOT NULL |  | 
  xac_nhan_boi | bigint | NULL |  | 
  xac_nhan_luc | datetime2 | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__exam_res__3213E83F6131BDEE PRIMARY KEY (id)
 
Other Indexes:
  uq_exam_results_bai_thi_id (bai_thi_id) NONCLUSTERED UNIQUE
  ix_exam_results_hoc_vien_id (hoc_vien_id) NONCLUSTERED
  ix_exam_results_ket_qua (ket_qua) NONCLUSTERED
  ix_exam_results_xac_nhan_luc (xac_nhan_luc) NONCLUSTERED
 
Foreign Keys:
  fk_exam_results_bai_thi FOREIGN KEY (bai_thi_id) REFERENCES [dbo].[bai_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_exam_results_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_exam_results_xac_nhan_boi FOREIGN KEY (xac_nhan_boi) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_exam_results_tong_so_cau CHECK (([tong_so_cau]>=(0)))
  ck_exam_results_so_cau_dung CHECK (([so_cau_dung]>=(0)))
  ck_exam_results_diem CHECK (([diem]>=(0)))
  ck_exam_results_ket_qua CHECK (([ket_qua]='khong_dat' OR [ket_qua]='dat'))
  ck_exam_results_so_cau CHECK (([so_cau_dung]<=[tong_so_cau]))
 


--------------------------------------------------------------------------------
Table: dbo.file_usages
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  file_id | bigint | NOT NULL |  | 
  entity_name | varchar(50) | NOT NULL |  | 
  entity_id | bigint | NOT NULL |  | 
  field_name | varchar(50) | NOT NULL |  | 
  is_primary | bit | NOT NULL | ((0)) | 
  sort_order | int | NOT NULL | ((0)) | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__file_usa__3213E83FFA7223C9 PRIMARY KEY (id)
 
Other Indexes:
  uq_file_usages (file_id, entity_name, entity_id, field_name) NONCLUSTERED UNIQUE
  ix_file_usages_entity (entity_name, entity_id) NONCLUSTERED
  ix_file_usages_file_id (file_id) NONCLUSTERED
 
Foreign Keys:
  fk_fu_file FOREIGN KEY (file_id) REFERENCES [dbo].[files](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_file_usages_sort_order CHECK (([sort_order]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.files
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  storage_provider | varchar(30) | NOT NULL |  | 
  bucket_name | varchar(100) | NULL |  | 
  object_key | varchar(500) | NOT NULL |  | 
  public_url | varchar(1000) | NOT NULL |  | 
  file_name | nvarchar(255) | NOT NULL |  | 
  mime_type | varchar(100) | NOT NULL |  | 
  size_bytes | bigint | NOT NULL |  | 
  checksum_sha256 | varchar(128) | NULL |  | 
  width | int | NULL |  | 
  height | int | NULL |  | 
  duration_seconds | int | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('active') | 
  created_by | bigint | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__files__3213E83FA139E150 PRIMARY KEY (id)
 
Other Indexes:
  ix_files_storage_provider (storage_provider) NONCLUSTERED
  ix_files_created_at (created_at) NONCLUSTERED
  ix_files_created_by (created_by) NONCLUSTERED
 
Foreign Keys:
  fk_files_created_by FOREIGN KEY (created_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_files_size_bytes CHECK (([size_bytes]>=(0)))
  ck_files_dimensions CHECK ((([width] IS NULL OR [width]>=(0)) AND ([height] IS NULL OR [height]>=(0))))
  ck_files_duration CHECK (([duration_seconds] IS NULL OR [duration_seconds]>=(0)))
  ck_files_storage_provider CHECK (([storage_provider]='gcs' OR [storage_provider]='azure_blob' OR [storage_provider]='cloudinary' OR [storage_provider]='s3' OR [storage_provider]='local'))
  ck_files_trang_thai CHECK (([trang_thai]='deleted' OR [trang_thai]='archived' OR [trang_thai]='active'))
 


--------------------------------------------------------------------------------
Table: dbo.goi_quyen
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_goi | varchar(50) | NOT NULL |  | 
  ten_goi | nvarchar(150) | NOT NULL |  | 
  mo_ta | nvarchar(500) | NULL |  | 
  is_active | bit | NOT NULL | ((1)) | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__goi_quye__3213E83FF4E7B77B PRIMARY KEY (id)
 
Other Indexes:
  uq_goi_quyen_ma_goi (ma_goi) NONCLUSTERED UNIQUE
 


--------------------------------------------------------------------------------
Table: dbo.giay_to_dinh_kem
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ho_so_id | bigint | NOT NULL |  | 
  ten_giay_to | nvarchar(150) | NOT NULL |  | 
  duong_dan_file | varchar(255) | NOT NULL |  | 
  loai_file | varchar(20) | NULL |  | 
  ngay_tai_len | datetime2 | NOT NULL | (getdate()) | 
  trang_thai | varchar(30) | NOT NULL | ('hop_le') | 
 
Indexes / Primary Keys:
  PK__giay_to___3213E83F329977FA PRIMARY KEY (id)
 
Foreign Keys:
  fk_giay_to_ho_so FOREIGN KEY (ho_so_id) REFERENCES [dbo].[ho_so_dang_ky](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.ho_so_dang_ky
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  hoc_vien_id | bigint | NOT NULL |  | 
  ma_ho_so | varchar(30) | NOT NULL |  | 
  ngay_nop | datetime2 | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('cho_nop') | 
  ghi_chu | nvarchar(500) | NULL |  | 
  nguoi_duyet_id | bigint | NULL |  | 
  ngay_duyet | datetime2 | NULL |  | 
 
Indexes / Primary Keys:
  PK__ho_so_da__3213E83F86ADE249 PRIMARY KEY (id)
 
Other Indexes:
  uq_ho_so_ma_ho_so (ma_ho_so) NONCLUSTERED UNIQUE
  ix_ho_so_dang_ky_hoc_vien_id (hoc_vien_id) NONCLUSTERED
 
Foreign Keys:
  fk_ho_so_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ho_so_nguoi_duyet FOREIGN KEY (nguoi_duyet_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.hoc_vien
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  nguoi_dung_id | bigint | NOT NULL |  | 
  ho_ten | nvarchar(150) | NOT NULL |  | 
  ngay_sinh | date | NULL |  | 
  gioi_tinh | nvarchar(10) | NULL |  | 
  cccd | varchar(20) | NULL |  | 
  dia_chi | nvarchar(255) | NULL |  | 
  anh_chan_dung | varchar(255) | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__hoc_vien__3213E83F87831D0F PRIMARY KEY (id)
 
Other Indexes:
  uq_hoc_vien_cccd (cccd) NONCLUSTERED UNIQUE
  uq_hoc_vien_nguoi_dung (nguoi_dung_id) NONCLUSTERED UNIQUE
  ix_hoc_vien_nguoi_dung_id (nguoi_dung_id) NONCLUSTERED
 
Foreign Keys:
  fk_hoc_vien_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.ky_thi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_ky_thi | varchar(30) | NOT NULL |  | 
  ten_ky_thi | nvarchar(150) | NOT NULL |  | 
  ngay_thi | date | NOT NULL |  | 
  mo_ta | nvarchar(255) | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('sap_dien_ra') | 
 
Indexes / Primary Keys:
  PK__ky_thi__3213E83F50D275CD PRIMARY KEY (id)
 
Other Indexes:
  uq_ky_thi_ma (ma_ky_thi) NONCLUSTERED UNIQUE
  ix_ky_thi_ngay_thi (ngay_thi) NONCLUSTERED
 


--------------------------------------------------------------------------------
Table: dbo.khoa_hoc
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_khoa_hoc | varchar(30) | NOT NULL |  | 
  ten_khoa_hoc | nvarchar(150) | NOT NULL |  | 
  mo_ta | nvarchar(500) | NULL |  | 
  hoc_phi | decimal(18,2) | NOT NULL | ((0)) | 
  thoi_luong | int | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('dang_mo') | 
 
Indexes / Primary Keys:
  PK__khoa_hoc__3213E83FCFDB0556 PRIMARY KEY (id)
 
Other Indexes:
  uq_khoa_hoc_ma (ma_khoa_hoc) NONCLUSTERED UNIQUE
 
Check Constraints:
  ck_khoa_hoc_hoc_phi CHECK (([hoc_phi]>=(0)))
  ck_khoa_hoc_thoi_luong CHECK (([thoi_luong] IS NULL OR [thoi_luong]>(0)))
 


--------------------------------------------------------------------------------
Table: dbo.loai_khoan_thu
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_loai | varchar(30) | NOT NULL |  | 
  ten_loai | nvarchar(150) | NOT NULL |  | 
  so_tien_mac_dinh | decimal(18,2) | NOT NULL | ((0)) | 
  mo_ta | nvarchar(255) | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('hoat_dong') | 
 
Indexes / Primary Keys:
  PK__loai_kho__3213E83F31B2719F PRIMARY KEY (id)
 
Other Indexes:
  uq_loai_khoan_thu_ma (ma_loai) NONCLUSTERED UNIQUE
 
Check Constraints:
  ck_loai_khoan_thu_so_tien CHECK (([so_tien_mac_dinh]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.loai_nguoi_dung
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_loai | varchar(30) | NOT NULL |  | 
  ten_loai | nvarchar(100) | NOT NULL |  | 
  mo_ta | nvarchar(255) | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__loai_ngu__3213E83F09412A08 PRIMARY KEY (id)
 
Other Indexes:
  uq_loai_nguoi_dung_ma_loai (ma_loai) NONCLUSTERED UNIQUE
 


--------------------------------------------------------------------------------
Table: dbo.loai_vi_pham
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_loai | varchar(30) | NOT NULL |  | 
  ten_loai | nvarchar(150) | NOT NULL |  | 
  mo_ta | nvarchar(255) | NULL |  | 
  muc_xu_ly_mac_dinh | nvarchar(255) | NULL |  | 
 
Indexes / Primary Keys:
  PK__loai_vi___3213E83F778BD438 PRIMARY KEY (id)
 
Other Indexes:
  uq_loai_vi_pham_ma (ma_loai) NONCLUSTERED UNIQUE
 


--------------------------------------------------------------------------------
Table: dbo.lop_hoc
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  khoa_hoc_id | bigint | NOT NULL |  | 
  ma_lop | varchar(30) | NOT NULL |  | 
  ten_lop | nvarchar(150) | NOT NULL |  | 
  giao_vien_id | bigint | NULL |  | 
  ngay_bat_dau | date | NULL |  | 
  ngay_ket_thuc | date | NULL |  | 
  si_so_toi_da | int | NOT NULL | ((0)) | 
  trang_thai | varchar(30) | NOT NULL | ('dang_mo') | 
  so_thu_tu | int | NULL |  | 
  giao_vien_ho_so_id | bigint | NULL |  | 
 
Indexes / Primary Keys:
  PK__lop_hoc__3213E83F7634754B PRIMARY KEY (id)
 
Other Indexes:
  uq_lop_hoc_ma (ma_lop) NONCLUSTERED UNIQUE
  ix_lop_hoc_khoa_hoc_id (khoa_hoc_id) NONCLUSTERED
  ix_lop_hoc_giao_vien_ho_so (giao_vien_ho_so_id) NONCLUSTERED
  uq_lop_hoc_khoa_hoc_so_thu_tu (khoa_hoc_id, so_thu_tu) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_lop_hoc_khoa_hoc FOREIGN KEY (khoa_hoc_id) REFERENCES [dbo].[khoa_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_lop_hoc_giao_vien FOREIGN KEY (giao_vien_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_lop_hoc_giao_vien_ho_so FOREIGN KEY (giao_vien_ho_so_id) REFERENCES [dbo].[giao_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_lop_hoc_si_so CHECK (([si_so_toi_da]>=(0)))
  ck_lop_hoc_ngay CHECK (([ngay_ket_thuc] IS NULL OR [ngay_bat_dau] IS NULL OR [ngay_ket_thuc]>=[ngay_bat_dau]))
 


--------------------------------------------------------------------------------
Table: dbo.lop_hoc_hoc_vien
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  lop_hoc_id | bigint | NOT NULL |  | 
  hoc_vien_id | bigint | NOT NULL |  | 
  ngay_vao_lop | date | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('dang_hoc') | 
 
Indexes / Primary Keys:
  PK__lop_hoc___3213E83F69C736EA PRIMARY KEY (id)
 
Other Indexes:
  uq_lop_hoc_hoc_vien (lop_hoc_id, hoc_vien_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_lhhv_lop_hoc FOREIGN KEY (lop_hoc_id) REFERENCES [dbo].[lop_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_lhhv_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.nguoi_dung
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ten_dang_nhap | varchar(50) | NOT NULL |  | 
  mat_khau_hash | varchar(255) | NOT NULL |  | 
  email | varchar(100) | NOT NULL |  | 
  so_dien_thoai | varchar(20) | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('hoat_dong') | 
  lan_dang_nhap_cuoi | datetime2 | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__nguoi_du__3213E83F8FE08FD6 PRIMARY KEY (id)
 
Other Indexes:
  uq_nguoi_dung_email (email) NONCLUSTERED UNIQUE
  uq_nguoi_dung_ten_dang_nhap (ten_dang_nhap) NONCLUSTERED UNIQUE
 


--------------------------------------------------------------------------------
Table: dbo.nguoi_dung_loai
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  nguoi_dung_id | bigint | NOT NULL |  | 
  loai_nguoi_dung_id | bigint | NOT NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__nguoi_du__3213E83FB4E4FBEF PRIMARY KEY (id)
 
Other Indexes:
  uq_nguoi_dung_loai (nguoi_dung_id, loai_nguoi_dung_id) NONCLUSTERED UNIQUE
  ix_nguoi_dung_loai_nguoi_dung_id (nguoi_dung_id) NONCLUSTERED
  ix_nguoi_dung_loai_loai_id (loai_nguoi_dung_id) NONCLUSTERED
 
Foreign Keys:
  fk_ndl_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ndl_loai_nguoi_dung FOREIGN KEY (loai_nguoi_dung_id) REFERENCES [dbo].[loai_nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.nguoi_dung_vai_tro
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  nguoi_dung_id | bigint | NOT NULL |  | 
  vai_tro_id | bigint | NOT NULL |  | 
 
Indexes / Primary Keys:
  PK__nguoi_du__3213E83FE2B3FCB2 PRIMARY KEY (id)
 
Other Indexes:
  uq_ndvt (nguoi_dung_id, vai_tro_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_ndvt_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_ndvt_vai_tro FOREIGN KEY (vai_tro_id) REFERENCES [dbo].[vai_tro](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.nhat_ky_he_thong
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  nguoi_dung_id | bigint | NULL |  | 
  hanh_dong | nvarchar(100) | NOT NULL |  | 
  bang_tac_dong | varchar(100) | NULL |  | 
  khoa_chinh_du_lieu | bigint | NULL |  | 
  noi_dung | nvarchar | NULL |  | 
  ip_address | varchar(45) | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__nhat_ky___3213E83F0E70893F PRIMARY KEY (id)
 
Foreign Keys:
  fk_nhat_ky_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.post_categories
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  post_id | bigint | NOT NULL |  | 
  category_id | bigint | NOT NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__post_cat__3213E83FBA6A40E9 PRIMARY KEY (id)
 
Other Indexes:
  uq_post_categories (post_id, category_id) NONCLUSTERED UNIQUE
  ix_post_categories_post_id (post_id) NONCLUSTERED
  ix_post_categories_category_id (category_id) NONCLUSTERED
 
Foreign Keys:
  fk_post_categories_post FOREIGN KEY (post_id) REFERENCES [dbo].[posts](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_post_categories_category FOREIGN KEY (category_id) REFERENCES [dbo].[categories](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.posts
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_bai_viet | varchar(50) | NOT NULL |  | 
  title | nvarchar(255) | NOT NULL |  | 
  slug | varchar(255) | NOT NULL |  | 
  summary | nvarchar(1000) | NULL |  | 
  content | nvarchar | NOT NULL |  | 
  post_type | varchar(30) | NOT NULL |  | 
  thumbnail_file_id | bigint | NULL |  | 
  meta_title | nvarchar(255) | NULL |  | 
  meta_description | nvarchar(500) | NULL |  | 
  canonical_url | varchar(500) | NULL |  | 
  published_at | datetime2 | NULL |  | 
  trang_thai | varchar(30) | NOT NULL | ('draft') | 
  author_id | bigint | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__posts__3213E83FFE00FFDE PRIMARY KEY (id)
 
Other Indexes:
  uq_posts_slug (slug) NONCLUSTERED UNIQUE
  uq_posts_ma_bai_viet (ma_bai_viet) NONCLUSTERED UNIQUE
  ix_posts_post_type (post_type) NONCLUSTERED
  ix_posts_trang_thai (trang_thai) NONCLUSTERED
  ix_posts_published_at (published_at) NONCLUSTERED
  ix_posts_author_id (author_id) NONCLUSTERED
 
Foreign Keys:
  fk_posts_thumbnail_file FOREIGN KEY (thumbnail_file_id) REFERENCES [dbo].[files](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_posts_author FOREIGN KEY (author_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_posts_post_type CHECK (([post_type]='huong_dan' OR [post_type]='khoa_hoc' OR [post_type]='tin_tuc' OR [post_type]='gioi_thieu'))
  ck_posts_trang_thai CHECK (([trang_thai]='archived' OR [trang_thai]='published' OR [trang_thai]='draft'))
 


--------------------------------------------------------------------------------
Table: dbo.phien_on_tap
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  hoc_vien_id | bigint | NOT NULL |  | 
  ngay_tao | datetime2 | NOT NULL | (getdate()) | 
  thoi_gian_bat_dau | datetime2 | NULL |  | 
  thoi_gian_nop | datetime2 | NULL |  | 
  tong_so_cau | int | NOT NULL | ((0)) | 
  so_cau_dung | int | NOT NULL | ((0)) | 
  diem | decimal(5,2) | NOT NULL | ((0)) | 
  trang_thai | varchar(30) | NOT NULL | ('moi_tao') | 
 
Indexes / Primary Keys:
  PK__phien_on__3213E83FA7981BFE PRIMARY KEY (id)
 
Other Indexes:
  ix_phien_on_tap_hoc_vien_id (hoc_vien_id) NONCLUSTERED
 
Foreign Keys:
  fk_phien_on_tap_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_phien_on_tap_tong_so_cau CHECK (([tong_so_cau]>=(0)))
  ck_phien_on_tap_so_cau_dung CHECK (([so_cau_dung]>=(0)))
  ck_phien_on_tap_diem CHECK (([diem]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.phien_on_tap_cau_hoi
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  phien_on_tap_id | bigint | NOT NULL |  | 
  cau_hoi_id | bigint | NOT NULL |  | 
  dap_an_chon_id | bigint | NULL |  | 
  la_dung | bit | NULL |  | 
  thu_tu_cau | int | NOT NULL |  | 
 
Indexes / Primary Keys:
  PK__phien_on__3213E83FDB72CDFB PRIMARY KEY (id)
 
Other Indexes:
  uq_phien_on_tap_thu_tu (phien_on_tap_id, thu_tu_cau) NONCLUSTERED UNIQUE
  uq_phien_on_tap_cau_hoi (phien_on_tap_id, cau_hoi_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_pot_ch_phien_on_tap FOREIGN KEY (phien_on_tap_id) REFERENCES [dbo].[phien_on_tap](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_pot_ch_cau_hoi FOREIGN KEY (cau_hoi_id) REFERENCES [dbo].[cau_hoi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_pot_ch_dap_an FOREIGN KEY (dap_an_chon_id) REFERENCES [dbo].[dap_an](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.phieu_thu
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_phieu_thu | varchar(30) | NOT NULL |  | 
  hoc_vien_id | bigint | NOT NULL |  | 
  ngay_thu | datetime2 | NOT NULL | (getdate()) | 
  tong_tien | decimal(18,2) | NOT NULL | ((0)) | 
  trang_thai | varchar(30) | NOT NULL | ('cho_xac_nhan') | 
  nguoi_lap_id | bigint | NULL |  | 
  nguoi_xac_nhan_id | bigint | NULL |  | 
  dang_ky_khoa_hoc_id | bigint | NULL |  | 
  phuong_thuc_thanh_toan | varchar(30) | NULL |  | 
  ngay_xac_nhan | datetime2 | NULL |  | 
 
Indexes / Primary Keys:
  PK__phieu_th__3213E83F8AE4D036 PRIMARY KEY (id)
 
Other Indexes:
  uq_phieu_thu_ma (ma_phieu_thu) NONCLUSTERED UNIQUE
  ix_phieu_thu_hoc_vien_id (hoc_vien_id) NONCLUSTERED
  ix_phieu_thu_dang_ky_khoa_hoc (dang_ky_khoa_hoc_id, trang_thai) NONCLUSTERED
 
Foreign Keys:
  fk_phieu_thu_dang_ky_khoa_hoc FOREIGN KEY (dang_ky_khoa_hoc_id) REFERENCES [dbo].[dang_ky_khoa_hoc](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_phieu_thu_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_phieu_thu_nguoi_lap FOREIGN KEY (nguoi_lap_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_phieu_thu_nguoi_xac_nhan FOREIGN KEY (nguoi_xac_nhan_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_phieu_thu_tong_tien CHECK (([tong_tien]>=(0)))
 


--------------------------------------------------------------------------------
Table: dbo.quyen_han
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_quyen | varchar(50) | NOT NULL |  | 
  ten_quyen | nvarchar(100) | NOT NULL |  | 
  mo_ta | nvarchar(255) | NULL |  | 
 
Indexes / Primary Keys:
  PK__quyen_ha__3213E83F56C62BE3 PRIMARY KEY (id)
 
Other Indexes:
  uq_quyen_han_ma_quyen (ma_quyen) NONCLUSTERED UNIQUE
 


--------------------------------------------------------------------------------
Table: dbo.quyen_su_dung
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  nguoi_dung_id | bigint | NOT NULL |  | 
  goi_quyen_id | bigint | NOT NULL |  | 
  ngay_hieu_luc | datetime2 | NOT NULL |  | 
  ngay_het_han | datetime2 | NULL |  | 
  nguon_cap | varchar(30) | NOT NULL |  | 
  trang_thai | varchar(30) | NOT NULL |  | 
  ghi_chu | nvarchar(500) | NULL |  | 
  created_by | bigint | NULL |  | 
  created_at | datetime2 | NOT NULL | (getdate()) | 
  updated_at | datetime2 | NOT NULL | (getdate()) | 
 
Indexes / Primary Keys:
  PK__quyen_su__3213E83F1093E783 PRIMARY KEY (id)
 
Other Indexes:
  ix_qsd_nguoi_dung_trang_thai (nguoi_dung_id, trang_thai) NONCLUSTERED
  ix_qsd_ngay_het_han (ngay_het_han) NONCLUSTERED
  ix_qsd_goi_quyen_id (goi_quyen_id) NONCLUSTERED
 
Foreign Keys:
  fk_qsd_nguoi_dung FOREIGN KEY (nguoi_dung_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_qsd_goi_quyen FOREIGN KEY (goi_quyen_id) REFERENCES [dbo].[goi_quyen](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_qsd_created_by FOREIGN KEY (created_by) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 
Check Constraints:
  ck_qsd_nguon_cap CHECK (([nguon_cap]='promo' OR [nguon_cap]='manual' OR [nguon_cap]='payment'))
  ck_qsd_trang_thai CHECK (([trang_thai]='revoked' OR [trang_thai]='expired' OR [trang_thai]='active'))
  ck_qsd_ngay CHECK (([ngay_het_han] IS NULL OR [ngay_het_han]>=[ngay_hieu_luc]))
 


--------------------------------------------------------------------------------
Table: dbo.vai_tro
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  ma_vai_tro | varchar(30) | NOT NULL |  | 
  ten_vai_tro | nvarchar(100) | NOT NULL |  | 
  mo_ta | nvarchar(255) | NULL |  | 
 
Indexes / Primary Keys:
  PK__vai_tro__3213E83F71302517 PRIMARY KEY (id)
 
Other Indexes:
  uq_vai_tro_ma_vai_tro (ma_vai_tro) NONCLUSTERED UNIQUE
 


--------------------------------------------------------------------------------
Table: dbo.vai_tro_quyen_han
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  vai_tro_id | bigint | NOT NULL |  | 
  quyen_han_id | bigint | NOT NULL |  | 
 
Indexes / Primary Keys:
  PK__vai_tro___3213E83F245E98C4 PRIMARY KEY (id)
 
Other Indexes:
  uq_vtqh (vai_tro_id, quyen_han_id) NONCLUSTERED UNIQUE
 
Foreign Keys:
  fk_vtqh_vai_tro FOREIGN KEY (vai_tro_id) REFERENCES [dbo].[vai_tro](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_vtqh_quyen_han FOREIGN KEY (quyen_han_id) REFERENCES [dbo].[quyen_han](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 


--------------------------------------------------------------------------------
Table: dbo.vi_pham_quy_che
--------------------------------------------------------------------------------
  id | bigint | NOT NULL |  | IDENTITY
  hoc_vien_id | bigint | NOT NULL |  | 
  bai_thi_id | bigint | NULL |  | 
  loai_vi_pham_id | bigint | NOT NULL |  | 
  nguoi_ghi_nhan_id | bigint | NULL |  | 
  thoi_gian_vi_pham | datetime2 | NOT NULL | (getdate()) | 
  mo_ta | nvarchar(500) | NULL |  | 
  hinh_thuc_xu_ly | nvarchar(255) | NULL |  | 
 
Indexes / Primary Keys:
  PK__vi_pham___3213E83FBC48DE5C PRIMARY KEY (id)
 
Other Indexes:
  ix_vi_pham_hoc_vien_id (hoc_vien_id) NONCLUSTERED
 
Foreign Keys:
  fk_vpqc_hoc_vien FOREIGN KEY (hoc_vien_id) REFERENCES [dbo].[hoc_vien](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_vpqc_bai_thi FOREIGN KEY (bai_thi_id) REFERENCES [dbo].[bai_thi](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_vpqc_loai_vi_pham FOREIGN KEY (loai_vi_pham_id) REFERENCES [dbo].[loai_vi_pham](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
  fk_vpqc_nguoi_ghi_nhan FOREIGN KEY (nguoi_ghi_nhan_id) REFERENCES [dbo].[nguoi_dung](id) ON UPDATE NO_ACTION ON DELETE NO_ACTION
 



Completion time: 2026-05-07T05:59:30.2529746+07:00
