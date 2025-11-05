using MongoDB.Bson.Serialization.Attributes;

namespace TrustFlow.Core.Models
{
    public class RolePermission : BaseEntity
    {
        [BsonElement("RoleName")]
        public string RoleName { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; }

        [BsonElement("CanCreateProject")]
        public bool CanCreateProject { get; set; }

        [BsonElement("CanEditProject")]
        public bool CanEditProject { get; set; }

        [BsonElement("CanDeleteProject")]
        public bool CanDeleteProject { get; set; }

        [BsonElement("CanCreateBug")]
        public bool CanCreateBug { get; set; }

        [BsonElement("CanEditBug")]
        public bool CanEditBug { get; set; }

        [BsonElement("CanChangeBugStatus")]
        public bool CanChangeBugStatus { get; set; }

        [BsonElement("CanCommentOnBugs")]
        public bool CanCommentOnBugs { get; set; }

        [BsonElement("CanManageAdminSettings")]
        public bool CanManageAdminSettings { get; set; }
    }
}
